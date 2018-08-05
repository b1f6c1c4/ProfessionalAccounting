const path = require('path');
const fs = require('fs');
const webpack = require('webpack');
const logger = require('../../server/logger');
const { dllPlugin } = require('../../package.json');

function dependencyHandlers() {
  if (!dllPlugin) return [];

  const dllPath = path.resolve(process.cwd(), dllPlugin.path);

  const manifestPath = path.resolve(dllPath, 'main.json');

  if (!fs.existsSync(manifestPath)) {
    logger.error('The DLL manifest is missing. Please run `yarn build:dll`');
    process.exit(0);
  }

  return [
    new webpack.DllReferencePlugin({
      context: process.cwd(),
      // eslint-disable-next-line global-require, import/no-dynamic-require
      manifest: require(manifestPath),
    }),
  ];
}

module.exports = require('./webpack.base')({
  mode: 'development',

  // Add hot reloading in development
  entry: {
    mock: [
      'file-loader?name=assets/[name].[ext]!outdatedbrowser/outdatedbrowser/outdatedbrowser.min.css',
      'file-loader?name=assets/[name].[ext]!outdatedbrowser/outdatedbrowser/outdatedbrowser.min.js',
    ],
    index: [
      'webpack-hot-middleware/client?reload=true',
      'index/style.js',
    ],
    app: [
      'webpack-hot-middleware/client?reload=true',
      'root.js',
    ],
  },

  inject: true,

  output: {
    path: '/tmp/ansys-moe', // Imaginary path
    filename: 'assets/[name].js',
    chunkFilename: 'assets/[name].chunk.js',
    // [#6642](https://github.com/webpack/webpack/issues/6642)
    globalObject: 'this',
  },

  optimization: {
    noEmitOnErrors: true,
  },

  // Add development plugins
  plugins: [
    ...dependencyHandlers(),
    new webpack.HotModuleReplacementPlugin(),
  ],

  // Emit a source map for easier debugging
  // See https://webpack.js.org/configuration/devtool/#devtool
  devtool: 'eval-source-map',

  performance: {
    hints: false,
  },
});
