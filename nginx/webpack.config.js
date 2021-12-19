/* Copyright (C) 2020-2021 Iori Oikawa
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

const path = require('path');
const webpack = require('webpack');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const CopyPlugin = require('copy-webpack-plugin');
const { BundleAnalyzerPlugin } = require('webpack-bundle-analyzer');

const plugins = [
    new HtmlWebpackPlugin({
        filename: 'gui.html',
        template: 'gui.html',
    }),
    new webpack.EnvironmentPlugin({
        API_URL: '/api',
    }),
    new CopyPlugin({
        patterns: [
            { from: 'index-desktop.html', to: '.' },
            { from: 'index-mobile.html', to: '.' },
            { from: 'src/index.css', to: '.' },
            { from: 'node_modules/p5/lib/p5.min.js', to: '.' },
            { from: 'node_modules/dayjs/dayjs.min.js', to: '.' },
            { from: 'node_modules/modern-normalize/modern-normalize.css', to: '.' },
        ],
    }),
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
            '.',
            'art/',
            'src/',
            'node_modules/p5/lib/',
            'node_modules/dayjs/',
        ],
    },
};
