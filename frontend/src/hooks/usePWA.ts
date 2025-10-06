import { useState, useEffect, useCallback } from 'react';

interface BeforeInstallPromptEvent extends Event {
  readonly platforms: string[];
  readonly userChoice: Promise<{
    outcome: 'accepted' | 'dismissed';
    platform: string;
  }>;
  prompt(): Promise<void>;
}

declare global {
  interface WindowEventMap {
    beforeinstallprompt: BeforeInstallPromptEvent;
  }
}

interface PWAHookReturn {
  isInstallable: boolean;
  isInstalled: boolean;
  isOnline: boolean;
  promptInstall: () => Promise<void>;
  installPromptEvent: BeforeInstallPromptEvent | null;
}

export const usePWA = (): PWAHookReturn => {
  const [installPromptEvent, setInstallPromptEvent] = useState<BeforeInstallPromptEvent | null>(null);
  const [isInstallable, setIsInstallable] = useState(false);
  const [isInstalled, setIsInstalled] = useState(false);
  const [isOnline, setIsOnline] = useState(navigator.onLine);

  // Check if app is already installed
  useEffect(() => {
    const checkIfInstalled = () => {
      // Check if running in standalone mode (installed)
      const isStandalone = window.matchMedia('(display-mode: standalone)').matches;
      // Check if running as PWA in browser
      const isPWA = (window.navigator as any).standalone === true;
      setIsInstalled(isStandalone || isPWA);
    };

    checkIfInstalled();
  }, []);

  // Listen for beforeinstallprompt event
  useEffect(() => {
    const handleBeforeInstallPrompt = (e: BeforeInstallPromptEvent) => {
      console.log('PWA: beforeinstallprompt event fired');
      // Prevent the mini-infobar from appearing on mobile
      e.preventDefault();
      // Save the event so it can be triggered later
      setInstallPromptEvent(e);
      setIsInstallable(true);
    };

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt);

    return () => {
      window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
    };
  }, []);

  // Listen for appinstalled event
  useEffect(() => {
    const handleAppInstalled = () => {
      console.log('PWA: App was installed');
      setIsInstalled(true);
      setIsInstallable(false);
      setInstallPromptEvent(null);
    };

    window.addEventListener('appinstalled', handleAppInstalled);

    return () => {
      window.removeEventListener('appinstalled', handleAppInstalled);
    };
  }, []);

  // Listen for online/offline events
  useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  const promptInstall = useCallback(async () => {
    if (!installPromptEvent) {
      console.warn('PWA: Install prompt not available');
      return;
    }

    try {
      // Show the install prompt
      await installPromptEvent.prompt();
      
      // Wait for the user to respond to the prompt
      const { outcome } = await installPromptEvent.userChoice;
      
      console.log(`PWA: User ${outcome} the install prompt`);
      
      if (outcome === 'accepted') {
        setIsInstallable(false);
        setInstallPromptEvent(null);
      }
    } catch (error) {
      console.error('PWA: Error showing install prompt:', error);
    }
  }, [installPromptEvent]);

  return {
    isInstallable,
    isInstalled,
    isOnline,
    promptInstall,
    installPromptEvent,
  };
};

// Hook for service worker registration and updates
export const useServiceWorker = () => {
  const [isUpdateAvailable, setIsUpdateAvailable] = useState(false);
  const [registration, setRegistration] = useState<ServiceWorkerRegistration | null>(null);

  useEffect(() => {
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker
        .register('/sw.js')
        .then((reg) => {
          console.log('Service Worker registered:', reg);
          setRegistration(reg);

          // Listen for updates
          reg.addEventListener('updatefound', () => {
            const newWorker = reg.installing;
            if (newWorker) {
              newWorker.addEventListener('statechange', () => {
                if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                  // New content is available
                  setIsUpdateAvailable(true);
                }
              });
            }
          });
        })
        .catch((error) => {
          console.error('Service Worker registration failed:', error);
        });

      // Listen for messages from service worker
      navigator.serviceWorker.addEventListener('message', (event) => {
        if (event.data && event.data.type === 'SKIP_WAITING') {
          setIsUpdateAvailable(true);
        }
      });
    }
  }, []);

  const updateApp = useCallback(() => {
    if (registration && registration.waiting) {
      registration.waiting.postMessage({ type: 'SKIP_WAITING' });
      registration.waiting.addEventListener('statechange', () => {
        if (registration.waiting && registration.waiting.state === 'activated') {
          window.location.reload();
        }
      });
    }
  }, [registration]);

  return {
    isUpdateAvailable,
    updateApp,
    registration,
  };
};

// Hook for caching strategies
export const useOfflineData = <T>(key: string, fetcher: () => Promise<T>) => {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const [isFromCache, setIsFromCache] = useState(false);

  useEffect(() => {
    const loadData = async () => {
      try {
        setLoading(true);
        
        // Try to get cached data first
        const cachedData = localStorage.getItem(`offline_${key}`);
        if (cachedData) {
          setData(JSON.parse(cachedData));
          setIsFromCache(true);
          setLoading(false);
        }

        // Try to fetch fresh data if online
        if (navigator.onLine) {
          try {
            const freshData = await fetcher();
            setData(freshData);
            setIsFromCache(false);
            localStorage.setItem(`offline_${key}`, JSON.stringify(freshData));
          } catch (fetchError) {
            console.warn('Failed to fetch fresh data, using cached version');
            if (!cachedData) {
              throw fetchError;
            }
          }
        }
      } catch (err) {
        setError(err as Error);
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [key, fetcher]);

  const refresh = useCallback(async () => {
    if (navigator.onLine) {
      try {
        setLoading(true);
        const freshData = await fetcher();
        setData(freshData);
        setIsFromCache(false);
        localStorage.setItem(`offline_${key}`, JSON.stringify(freshData));
      } catch (err) {
        setError(err as Error);
      } finally {
        setLoading(false);
      }
    }
  }, [key, fetcher]);

  return {
    data,
    loading,
    error,
    isFromCache,
    refresh,
  };
};