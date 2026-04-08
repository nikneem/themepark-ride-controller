const gatewayUrl = process.env['ASPIRE_GATEWAY_URL'] || 'http://localhost:5000';

module.exports = {
  '/controlcenter': { target: gatewayUrl, secure: false, changeOrigin: true },
  '/rides': { target: gatewayUrl, secure: false, changeOrigin: true },
  '/queue': { target: gatewayUrl, secure: false, changeOrigin: true },
  '/maintenance': { target: gatewayUrl, secure: false, changeOrigin: true },
  '/weather': { target: gatewayUrl, secure: false, changeOrigin: true },
  '/mascots': { target: gatewayUrl, secure: false, changeOrigin: true },
  '/refunds': { target: gatewayUrl, secure: false, changeOrigin: true },
};
