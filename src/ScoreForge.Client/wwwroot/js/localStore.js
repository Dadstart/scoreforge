(() => {
    const dbName = "scoreforge";
    const storeName = "localstore";
    const version = 1;

    const openDb = () =>
        new Promise((resolve, reject) => {
            const request = indexedDB.open(dbName, version);

            request.onupgradeneeded = () => {
                const database = request.result;
                if (!database.objectStoreNames.contains(storeName))
                    database.createObjectStore(storeName, { keyPath: "key" });
            };

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });

    const run = async (mode, operation) => {
        const database = await openDb();
        try {
            return await new Promise((resolve, reject) => {
                const tx = database.transaction(storeName, mode);
                const store = tx.objectStore(storeName);
                operation(store, resolve, reject);
            });
        } finally {
            database.close();
        }
    };

    window.scoreForgeLocalStore = {
        async set(key, value) {
            await run("readwrite", (store, resolve, reject) => {
                const request = store.put({ key, value });
                request.onsuccess = () => resolve();
                request.onerror = () => reject(request.error);
            });
        },
        async get(key) {
            return await run("readonly", (store, resolve, reject) => {
                const request = store.get(key);
                request.onsuccess = () => resolve(request.result?.value ?? null);
                request.onerror = () => reject(request.error);
            });
        }
    };
})();
