/* Copyright (C) 2021-2025 Iori Oikawa
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
        filename: 'gui-discount.html',
        template: 'gui-discount.html',
        chunks: ['discount'],
    }),
    new HtmlWebpackPlugin({
        filename: 'gui-jtysf.html',
        template: 'gui-jtysf.html',
        chunks: ['jtysf'],
    }),
    new HtmlWebpackPlugin({
        filename: 'gui-taobao.html',
        template: 'gui-taobao.html',
        chunks: ['taobao'],
    }),
    new webpack.EnvironmentPlugin({
        API_URL: '/api',
    }),
    new CopyPlugin({
        patterns: [
            { from: 'index-desktop.html', to: '.' },
            { from: 'index-mobile.html', to: '.' },
            { from: 'invite.html', to: '.' },
            { from: 'login.html', to: '.' },
            { from: 'src/index.css', to: '.' },
            { from: 'src/taobao.css', to: '.' },
            { from: 'art/', to: '.' },
            { from: 'js/', to: 'js' },
            { from: 'public/', to: 'public' },
            { from: 'node_modules/p5/lib/p5.min.js', to: '.' },
            { from: 'node_modules/dayjs/dayjs.min.js', to: '.' },
            { from: 'node_modules/modern-normalize/modern-normalize.css', to: '.' },
            { from: 'node_modules/tuicss/dist/tuicss.min.css', to: '.' },
            { from: 'node_modules/tuicss/dist/images/', to: 'images' },
            { from: 'node_modules/autosize/dist/autosize.min.js', to: '.' },
            { from: 'node_modules/ace-builds/src-min/ace.js', to: '.' },
            { from: 'node_modules/ace-builds/src-min/theme-chrome.js', to: '.' },
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
    entry: {
        discount: ['./src/gui-discount.js'],
        jtysf: ['./src/gui-jtysf.js'],
        taobao: ['./src/gui-taobao.js'],
    },
    output: {
        filename: '[name].bundle.js',
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
            'node_modules/tuicss/dist/',
        ],
    },
};
