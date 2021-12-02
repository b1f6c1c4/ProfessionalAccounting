const path = require('path');
const webpack = require('webpack');
const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = {
  entry: './src/index.js',
  output: {
    filename: 'main.js',
    path: path.resolve(__dirname, 'dist'),
  },
  plugins: [
    new HtmlWebpackPlugin({
      title: '专业记账系统+',
      template: 'index-gui.html',
    }),
    new webpack.EnvironmentPlugin(['API_URL']),
  ],
  devServer: {
    static: 'art',
  }
};
