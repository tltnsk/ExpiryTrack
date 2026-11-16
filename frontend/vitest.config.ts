import { fileURLToPath } from "node:url";
import { mergeConfig, defineConfig } from 'vitest/config'
import viteConfig from "./vite.config";

export default mergeConfig(viteConfig, defineConfig({
    test: {
        environment: 'jsdom',
        coverage: {
            include: ['src/**/*.{ts,vue}'],
            exclude: ['src/__test__/**', 'src/main.ts'],
        },
        root: fileURLToPath(new URL('./', import.meta.url)),
    },
}))