const path = require('path');
const webpack = require('webpack');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const { BundleAnalyzerPlugin } = require('webpack-bundle-analyzer');

const plugins = [
    new HtmlWebpackPlugin({
      template: 'index-gui.html',
    }),
    new webpack.EnvironmentPlugin(['API_URL']),
];

if (process.env.ANALYZE) {
    plugins.push(new BundleAnalyzerPlugin({
        analyzerHost: '0.0.0.0',
        openAnalyzer: false,
    }));
}

module.exports = {
    entry: './src/index.js',
    output: {
        filename: 'main.js',
        path: path.resolve(__dirname, 'dist'),
    },
    externalsType: 'script',
    externals: {
        p5: ['/p5.min.js', 'p5'],
        dayjs: ['/dayjs.min.js', 'dayjs'],
    },
    plugins,
    devServer: {
        static: [
            'art/',
            'node_modules/p5/lib/',
            'node_modules/dayjs/',
        ],
    },
};
