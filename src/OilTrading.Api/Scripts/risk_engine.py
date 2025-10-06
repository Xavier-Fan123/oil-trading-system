#!/usr/bin/env python3
"""
Risk Calculation Engine for Oil Trading System
Implements GARCH(1,1) and Monte Carlo VaR calculations
"""

import json
import sys
import numpy as np
import pandas as pd
from scipy import stats
from typing import Dict, List, Tuple, Any
import warnings
warnings.filterwarnings('ignore')

# Try to import arch library, fallback if not available
try:
    from arch import arch_model
    ARCH_AVAILABLE = True
except ImportError:
    ARCH_AVAILABLE = False
    print("Warning: arch library not available, using simplified GARCH", file=sys.stderr)

class RiskEngine:
    def __init__(self, seed: int = 42):
        """Initialize risk engine with fixed random seed for reproducibility"""
        np.random.seed(seed)
        self.seed = seed
    
    def calculate_garch_var(self, 
                           positions: List[Dict],
                           returns: Dict[str, List[float]],
                           confidence_levels: List[float] = [0.95, 0.99]) -> Dict[str, float]:
        """
        Calculate VaR using GARCH(1,1) model with Student's t-distribution
        """
        try:
            portfolio_value = self._calculate_portfolio_value(positions)
            portfolio_returns = self._calculate_portfolio_returns(positions, returns)
            
            if len(portfolio_returns) < 100:
                # Not enough data for GARCH, use historical simulation
                return self._historical_var(portfolio_returns, portfolio_value, confidence_levels)
            
            if ARCH_AVAILABLE:
                # Fit GARCH(1,1) model
                returns_series = pd.Series(portfolio_returns) * 100  # Convert to percentage
                
                # Try Student's t distribution first, fallback to Normal if fitting fails
                try:
                    model = arch_model(returns_series, vol='GARCH', p=1, q=1, dist='t')
                    res = model.fit(disp='off', show_warning=False)
                    
                    # Forecast 1-day ahead
                    forecast = res.forecast(horizon=1)
                    predicted_variance = forecast.variance.values[-1, 0]
                    predicted_vol = np.sqrt(predicted_variance) / 100  # Convert back from percentage
                    
                    # Use fitted distribution parameters
                    if hasattr(res.params, 'nu'):  # Student's t parameter
                        nu = res.params['nu']
                        # Calculate VaR using Student's t distribution
                        var_95 = abs(stats.t.ppf(0.05, nu) * predicted_vol * portfolio_value)
                        var_99 = abs(stats.t.ppf(0.01, nu) * predicted_vol * portfolio_value)
                    else:
                        # Normal distribution
                        var_95 = abs(stats.norm.ppf(0.05) * predicted_vol * portfolio_value)
                        var_99 = abs(stats.norm.ppf(0.01) * predicted_vol * portfolio_value)
                    
                except Exception as e:
                    print(f"GARCH fitting failed, using Normal distribution: {e}", file=sys.stderr)
                    # Fallback to simple GARCH with Normal distribution
                    model = arch_model(returns_series, vol='GARCH', p=1, q=1, dist='normal')
                    res = model.fit(disp='off', show_warning=False)
                    
                    forecast = res.forecast(horizon=1)
                    predicted_variance = forecast.variance.values[-1, 0]
                    predicted_vol = np.sqrt(predicted_variance) / 100
                    
                    var_95 = abs(stats.norm.ppf(0.05) * predicted_vol * portfolio_value)
                    var_99 = abs(stats.norm.ppf(0.01) * predicted_vol * portfolio_value)
                
            else:
                # Simplified GARCH implementation without arch library
                var_95, var_99 = self._simplified_garch(portfolio_returns, portfolio_value)
            
            return {
                "var95": float(np.round(var_95)),
                "var99": float(np.round(var_99))
            }
            
        except Exception as e:
            print(f"Error in GARCH VaR calculation: {e}", file=sys.stderr)
            # Fallback to historical simulation
            return self._historical_var(portfolio_returns, portfolio_value, confidence_levels)
    
    def calculate_monte_carlo_var(self,
                                 positions: List[Dict],
                                 returns: Dict[str, List[float]],
                                 simulations: int = 100000,
                                 confidence_levels: List[float] = [0.95, 0.99]) -> Dict[str, float]:
        """
        Calculate VaR using Monte Carlo simulation
        """
        try:
            portfolio_value = self._calculate_portfolio_value(positions)
            
            # Calculate correlation matrix and individual statistics
            products = list(returns.keys())
            if len(products) == 0:
                return {"var95": 0.0, "var99": 0.0}
            
            # Convert returns to numpy array
            min_length = min(len(returns[p]) for p in products)
            returns_matrix = np.array([returns[p][:min_length] for p in products]).T
            
            # Calculate statistics
            mean_returns = np.mean(returns_matrix, axis=0)
            cov_matrix = np.cov(returns_matrix, rowvar=False)
            
            # Handle single product case
            if len(products) == 1:
                std_dev = np.sqrt(cov_matrix)
                simulated_returns = np.random.normal(mean_returns[0], std_dev, simulations)
                portfolio_sim_returns = simulated_returns
            else:
                # Generate correlated random returns
                try:
                    # Cholesky decomposition for correlated samples
                    L = np.linalg.cholesky(cov_matrix)
                    random_normals = np.random.randn(simulations, len(products))
                    correlated_returns = mean_returns + random_normals @ L.T
                    
                    # Calculate portfolio returns based on positions
                    portfolio_sim_returns = self._aggregate_simulated_returns(
                        positions, products, correlated_returns
                    )
                except np.linalg.LinAlgError:
                    # If Cholesky fails (non-positive definite), use eigenvalue decomposition
                    eigenvalues, eigenvectors = np.linalg.eig(cov_matrix)
                    eigenvalues = np.maximum(eigenvalues, 0)  # Ensure non-negative
                    L = eigenvectors @ np.diag(np.sqrt(eigenvalues))
                    random_normals = np.random.randn(simulations, len(products))
                    correlated_returns = mean_returns + random_normals @ L.T
                    
                    portfolio_sim_returns = self._aggregate_simulated_returns(
                        positions, products, correlated_returns
                    )
            
            # Calculate portfolio P&L
            portfolio_pnl = portfolio_sim_returns * portfolio_value
            
            # Sort and calculate VaR
            sorted_pnl = np.sort(portfolio_pnl)
            # CRITICAL FIX: Correct quantile calculation for VaR
            # Use numpy percentile function for precise quantile calculation
            var_95 = abs(np.percentile(sorted_pnl, 5))  # 5th percentile for 95% VaR
            var_99 = abs(np.percentile(sorted_pnl, 1))  # 1st percentile for 99% VaR
            
            return {
                "var95": float(np.round(var_95)),
                "var99": float(np.round(var_99))
            }
            
        except Exception as e:
            print(f"Error in Monte Carlo VaR calculation: {e}", file=sys.stderr)
            # Fallback to historical simulation
            portfolio_returns = self._calculate_portfolio_returns(positions, returns)
            return self._historical_var(portfolio_returns, portfolio_value, confidence_levels)
    
    def run_stress_tests(self,
                        positions: List[Dict],
                        current_prices: Dict[str, float]) -> List[Dict[str, Any]]:
        """
        Run predefined stress test scenarios
        """
        stress_results = []
        
        # Scenario 1: -10% price shock
        shock_10_down = self._calculate_stress_impact(positions, current_prices, -0.10)
        stress_results.append({
            "scenario": "-10% Shock",
            "pnlImpact": float(np.round(shock_10_down)),
            "description": "10% decline in all oil and fuel prices"
        })
        
        # Scenario 2: +10% price shock
        shock_10_up = self._calculate_stress_impact(positions, current_prices, 0.10)
        stress_results.append({
            "scenario": "+10% Shock",
            "pnlImpact": float(np.round(shock_10_up)),
            "description": "10% increase in all oil and fuel prices"
        })
        
        # Scenario 3: Historical worst (-15% as proxy)
        historical_worst = self._calculate_stress_impact(positions, current_prices, -0.15)
        stress_results.append({
            "scenario": "Historical Worst",
            "pnlImpact": float(np.round(historical_worst)),
            "description": "Repeat of historical worst daily oil price decline"
        })
        
        return stress_results
    
    # Helper methods
    def _calculate_portfolio_value(self, positions: List[Dict]) -> float:
        """Calculate total portfolio value"""
        total_value = 0
        for pos in positions:
            position_value = abs(pos['quantity'] * pos['lotSize'] * pos['currentPrice'])
            total_value += position_value
        return total_value
    
    def _calculate_portfolio_returns(self, 
                                    positions: List[Dict],
                                    returns: Dict[str, List[float]]) -> np.ndarray:
        """Calculate weighted portfolio returns"""
        if not returns:
            return np.array([])
        
        # Find minimum length of returns
        min_length = min(len(r) for r in returns.values())
        if min_length == 0:
            return np.array([])
        
        portfolio_returns = np.zeros(min_length)
        total_value = self._calculate_portfolio_value(positions)
        
        if total_value == 0:
            return portfolio_returns
        
        for pos in positions:
            product = pos['product']
            if product in returns:
                position_value = abs(pos['quantity'] * pos['lotSize'] * pos['currentPrice'])
                weight = position_value / total_value
                
                # Adjust returns for position direction
                position_returns = np.array(returns[product][:min_length])
                if pos['position'] == 'Short':
                    position_returns = -position_returns
                
                portfolio_returns += weight * position_returns
        
        return portfolio_returns
    
    def _historical_var(self, 
                       returns: np.ndarray,
                       portfolio_value: float,
                       confidence_levels: List[float]) -> Dict[str, float]:
        """Calculate historical VaR"""
        if len(returns) == 0:
            return {"var95": 0.0, "var99": 0.0}
        
        sorted_returns = np.sort(returns)
        
        var_95_index = int(len(sorted_returns) * 0.05)
        var_99_index = int(len(sorted_returns) * 0.01)
        
        var_95 = abs(sorted_returns[var_95_index] * portfolio_value)
        var_99 = abs(sorted_returns[var_99_index] * portfolio_value)
        
        return {
            "var95": float(np.round(var_95)),
            "var99": float(np.round(var_99))
        }
    
    def _simplified_garch(self, 
                         returns: np.ndarray,
                         portfolio_value: float) -> Tuple[float, float]:
        """Simplified GARCH implementation without arch library"""
        # Use exponentially weighted moving average as proxy
        lambda_param = 0.94  # RiskMetrics parameter
        
        # Calculate EWMA volatility
        squared_returns = returns ** 2
        weights = np.array([(1 - lambda_param) * lambda_param ** i 
                          for i in range(len(squared_returns))])
        weights = weights / weights.sum()
        
        variance = np.sum(weights * squared_returns[::-1])
        volatility = np.sqrt(variance)
        
        # Calculate VaR using normal distribution
        var_95 = abs(stats.norm.ppf(0.05) * volatility * portfolio_value)
        var_99 = abs(stats.norm.ppf(0.01) * volatility * portfolio_value)
        
        return var_95, var_99
    
    def _aggregate_simulated_returns(self,
                                    positions: List[Dict],
                                    products: List[str],
                                    simulated_returns: np.ndarray) -> np.ndarray:
        """Aggregate simulated returns based on portfolio positions"""
        portfolio_returns = np.zeros(len(simulated_returns))
        total_value = self._calculate_portfolio_value(positions)
        
        if total_value == 0:
            return portfolio_returns
        
        for pos in positions:
            if pos['product'] in products:
                idx = products.index(pos['product'])
                position_value = abs(pos['quantity'] * pos['lotSize'] * pos['currentPrice'])
                weight = position_value / total_value
                
                # Adjust for position direction
                if pos['position'] == 'Short':
                    portfolio_returns -= weight * simulated_returns[:, idx]
                else:
                    portfolio_returns += weight * simulated_returns[:, idx]
        
        return portfolio_returns
    
    def _calculate_stress_impact(self,
                                positions: List[Dict],
                                current_prices: Dict[str, float],
                                shock_percentage: float) -> float:
        """Calculate P&L impact of a price shock"""
        total_impact = 0
        
        for pos in positions:
            product = pos['product']
            if product in current_prices:
                current_price = current_prices[product]
                shocked_price = current_price * (1 + shock_percentage)
                price_change = shocked_price - current_price
                
                # Calculate P&L impact
                multiplier = 1 if pos['position'] == 'Long' else -1
                impact = price_change * pos['quantity'] * pos['lotSize'] * multiplier
                total_impact += impact
        
        return total_impact


def main():
    """Main entry point for the risk engine"""
    try:
        # Read input from stdin
        input_data = json.loads(sys.stdin.read())
        
        # Extract parameters
        method = input_data.get('method', 'garch')
        positions = input_data.get('positions', [])
        returns = input_data.get('returns', {})
        seed = input_data.get('seed', 42)
        simulations = input_data.get('simulations', 100000)
        
        # Initialize risk engine
        engine = RiskEngine(seed=seed)
        
        # Calculate based on method
        if method == 'garch':
            result = engine.calculate_garch_var(positions, returns)
        elif method == 'montecarlo':
            result = engine.calculate_monte_carlo_var(positions, returns, simulations)
        elif method == 'stress':
            current_prices = input_data.get('current_prices', {})
            result = {"stress_tests": engine.run_stress_tests(positions, current_prices)}
        else:
            result = {"error": f"Unknown method: {method}"}
        
        # Output result as JSON
        print(json.dumps(result))
        
    except Exception as e:
        error_result = {
            "error": str(e),
            "var95": 0.0,
            "var99": 0.0
        }
        print(json.dumps(error_result))
        sys.exit(1)


if __name__ == "__main__":
    main()