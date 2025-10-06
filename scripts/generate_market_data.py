#!/usr/bin/env python3
"""
Market Data Generator for Oil Trading System
Generates realistic historical price data for testing
"""

import pandas as pd
import numpy as np
from datetime import datetime, timedelta
import json
import sys
import os

# Set random seed for reproducibility
np.random.seed(42)

class MarketDataGenerator:
    def __init__(self):
        # Product configurations with realistic base prices (USD)
        self.products = {
            'BRENT': {
                'base_price': 85.00,
                'volatility': 0.025,  # 2.5% daily volatility
                'trend': 0.0001,      # Slight upward trend
                'name': 'Brent Crude Oil'
            },
            '380CST': {
                'base_price': 450.00,
                'volatility': 0.020,  # 2% daily volatility
                'trend': 0.0002,
                'name': 'Fuel Oil 380cst'
            },
            'MF05': {
                'base_price': 520.00,
                'volatility': 0.022,
                'trend': 0.0001,
                'name': 'Marine Fuel 0.5%'
            },
            'GASOIL': {
                'base_price': 680.00,
                'volatility': 0.023,
                'trend': 0.00015,
                'name': 'Gasoil'
            },
            'WTI': {
                'base_price': 80.00,
                'volatility': 0.025,
                'trend': 0.0001,
                'name': 'WTI Crude Oil'
            },
            'JET': {
                'base_price': 750.00,
                'volatility': 0.024,
                'trend': 0.0002,
                'name': 'Jet Fuel'
            }
        }
        
    def generate_price_series(self, product_code, days=365):
        """Generate realistic price series using Geometric Brownian Motion"""
        config = self.products[product_code]
        
        # Initialize arrays
        prices = np.zeros(days)
        returns = np.zeros(days)
        
        # Starting price
        prices[0] = config['base_price']
        
        # Generate daily returns
        for i in range(1, days):
            # Add some mean reversion
            mean_reversion = 0.01 * (config['base_price'] - prices[i-1]) / config['base_price']
            
            # Random shock
            random_shock = np.random.normal(0, config['volatility'])
            
            # Occasional large moves (fat tails)
            if np.random.random() < 0.05:  # 5% chance of large move
                random_shock *= 2.5
            
            # Calculate return
            daily_return = config['trend'] + mean_reversion + random_shock
            returns[i] = daily_return
            
            # Calculate new price
            prices[i] = prices[i-1] * (1 + daily_return)
            
            # Ensure price stays positive
            prices[i] = max(prices[i], config['base_price'] * 0.3)
        
        return prices, returns
    
    def generate_market_data(self, start_date=None, days=365):
        """Generate complete market data for all products"""
        if start_date is None:
            start_date = datetime.now() - timedelta(days=days)
        
        dates = pd.date_range(start=start_date, periods=days, freq='D')
        
        all_data = []
        
        for product_code, config in self.products.items():
            prices, returns = self.generate_price_series(product_code, days)
            
            for i, (date, price) in enumerate(zip(dates, prices)):
                # Skip weekends for more realistic data
                if date.weekday() < 5:  # Monday = 0, Sunday = 6
                    all_data.append({
                        'Id': str(np.random.uuid4()),
                        'ProductCode': product_code,
                        'ProductName': config['name'],
                        'PriceDate': date.strftime('%Y-%m-%d'),
                        'Price': round(price, 2),
                        'OpenPrice': round(price * np.random.uniform(0.98, 1.02), 2),
                        'HighPrice': round(price * np.random.uniform(1.00, 1.03), 2),
                        'LowPrice': round(price * np.random.uniform(0.97, 1.00), 2),
                        'ClosePrice': round(price, 2),
                        'Volume': np.random.randint(1000, 50000),
                        'Currency': 'USD',
                        'Unit': 'MT' if 'CST' in product_code or 'MF' in product_code else 'BBL',
                        'Source': 'Generated',
                        'CreatedAt': datetime.now().isoformat(),
                        'CreatedBy': 'DataGenerator'
                    })
        
        return pd.DataFrame(all_data)
    
    def save_to_excel(self, df, filename='market_data.xlsx'):
        """Save data to Excel file"""
        with pd.ExcelWriter(filename, engine='openpyxl') as writer:
            # Group by product for separate sheets
            for product in df['ProductCode'].unique():
                product_df = df[df['ProductCode'] == product].copy()
                product_df = product_df.sort_values('PriceDate')
                product_df.to_excel(writer, sheet_name=product, index=False)
            
            # Also create a combined sheet
            df_sorted = df.sort_values(['PriceDate', 'ProductCode'])
            df_sorted.to_excel(writer, sheet_name='All_Prices', index=False)
        
        print(f"✅ Excel file saved: {filename}")
    
    def save_to_json(self, df, filename='market_data.json'):
        """Save data to JSON file"""
        data = df.to_dict('records')
        with open(filename, 'w') as f:
            json.dump(data, f, indent=2, default=str)
        print(f"✅ JSON file saved: {filename}")
    
    def save_to_csv(self, df, filename='market_data.csv'):
        """Save data to CSV file"""
        df.to_csv(filename, index=False)
        print(f"✅ CSV file saved: {filename}")
    
    def generate_sql_inserts(self, df, filename='insert_market_data.sql'):
        """Generate SQL insert statements"""
        with open(filename, 'w') as f:
            f.write("-- Market Data Insert Statements\n")
            f.write("-- Generated: {}\n\n".format(datetime.now().isoformat()))
            
            for _, row in df.iterrows():
                sql = """INSERT INTO "MarketPrices" ("Id", "ProductCode", "ProductName", "PriceDate", "Price", "Currency", "Unit", "Source", "CreatedAt", "CreatedBy")
VALUES ('{Id}', '{ProductCode}', '{ProductName}', '{PriceDate}', {Price}, '{Currency}', '{Unit}', '{Source}', '{CreatedAt}', '{CreatedBy}');
""".format(**row)
                f.write(sql)
        
        print(f"✅ SQL file saved: {filename}")
    
    def print_statistics(self, df):
        """Print statistics about generated data"""
        print("\n📊 Generated Market Data Statistics:")
        print("=" * 50)
        print(f"Total Records: {len(df)}")
        print(f"Date Range: {df['PriceDate'].min()} to {df['PriceDate'].max()}")
        print(f"Products: {', '.join(df['ProductCode'].unique())}")
        print("\nPrice Statistics by Product:")
        print("-" * 50)
        
        for product in df['ProductCode'].unique():
            product_df = df[df['ProductCode'] == product]
            print(f"\n{product}:")
            print(f"  Records: {len(product_df)}")
            print(f"  Min Price: ${product_df['Price'].min():.2f}")
            print(f"  Max Price: ${product_df['Price'].max():.2f}")
            print(f"  Avg Price: ${product_df['Price'].mean():.2f}")
            print(f"  Std Dev: ${product_df['Price'].std():.2f}")
            
            # Calculate returns
            prices = product_df.sort_values('PriceDate')['Price'].values
            if len(prices) > 1:
                returns = np.diff(prices) / prices[:-1]
                print(f"  Daily Volatility: {np.std(returns)*100:.2f}%")
                print(f"  Annualized Volatility: {np.std(returns)*np.sqrt(252)*100:.2f}%")


def main():
    """Main function"""
    print("🛢️ Oil Trading System - Market Data Generator")
    print("=" * 50)
    
    # Create generator
    generator = MarketDataGenerator()
    
    # Generate data
    print("Generating market data...")
    df = generator.generate_market_data(days=365)
    
    # Create output directory
    output_dir = 'generated_data'
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)
    
    # Save in multiple formats
    generator.save_to_excel(df, os.path.join(output_dir, 'market_data.xlsx'))
    generator.save_to_csv(df, os.path.join(output_dir, 'market_data.csv'))
    generator.save_to_json(df, os.path.join(output_dir, 'market_data.json'))
    generator.generate_sql_inserts(df, os.path.join(output_dir, 'insert_market_data.sql'))
    
    # Print statistics
    generator.print_statistics(df)
    
    print("\n✅ Market data generation complete!")
    print(f"📁 Files saved in: {os.path.abspath(output_dir)}/")
    
    return df


if __name__ == "__main__":
    df = main()