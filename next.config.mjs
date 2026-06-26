import createNextIntlPlugin from 'next-intl/plugin';

const withNextIntl = createNextIntlPlugin();

/** @type {import('next').NextConfig} */
const nextConfig = {
  basePath: process.env.NEXT_PUBLIC_BASE_PATH || '',
  transpilePackages: ['@ajabadia/ecosystem-widgets', '@ajabadia/styles', '@ajabadia/satellite-sdk', 'next-intl'],
  serverExternalPackages: ['pandoc-wasm'],
  experimental: {
    outputFileTracingIncludes: {
      '/api/**/*': ['./node_modules/pandoc-wasm/src/pandoc.wasm'],
    },
  },
};

export default withNextIntl(nextConfig);
