const path = require('path');
const webpack = require('webpack');
const { dllPlugin } = require('../../package.json');
const base = require('./webpack.base');

if (!dllPlugin) { process.exit(0); }

const outputPath = path.join(process.cwd(), dllPlugin.path);

module.exports = base({
  mode: 'development',
  context: process.cwd(),
  entry: dllPlugin.dlls,
  devtool: 'eval',
  output: {
    filename: '[name].dll.js',
    path: outputPath,
    library: '[name]',
  },
  plugins: [
    new webpack.DllPlugin({
      name: '[name]',
      path: path.join(outputPath, '[name].json'),
    }),
  ],
  noHtml: true,
  performance: {
    hints: false,
  },
});
