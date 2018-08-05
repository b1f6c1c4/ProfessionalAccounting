const path = require('path');
const GitRevisionPlugin = require('git-revision-webpack-plugin');
const ExtractTextPlugin = require('extract-text-webpack-plugin');
// eslint-disable-next-line import/no-extraneous-dependencies
const UglifyJsPlugin = require('uglifyjs-webpack-plugin');
const {
  AdvancedInjectionPlugin,
  Prefetch,
  Preload,
  AsyncCss,
  InlineCss,
  Js,
} = require('advanced-injection-plugin');

const extractCss0 = new ExtractTextPlugin({
  filename: 'assets/[name].[sha1:contenthash:hex:8].css',
  allChunks: true,
});
const extractCss1 = new ExtractTextPlugin({
  filename: 'assets/[name].vendor.[sha1:contenthash:hex:8].css',
  allChunks: true,
});

const minify = {
  removeComments: true,
  collapseWhitespace: true,
  removeRedundantAttributes: true,
  useShortDoctype: true,
  removeEmptyAttributes: true,
  removeScriptTypeAttributes: true,
  removeStyleLinkTypeAttributes: true,
  keepClosingSlash: true,
  minifyJS: true,
  minifyCSS: true,
  minifyURLs: false,
};

module.exports = require('./webpack.base')({
  mode: 'production',

  // In production, we skip all hot-reloading stuff
  entry: {
    app: [
      'root.js',
    ],
  },

  babelOptions: {
    plugins: [
      'lodash',
    ],
  },

  cssLoaderVender: extractCss1.extract({
    fallback: 'style-loader',
    use: [{
      loader: 'css-loader',
      options: {
        minimize: true,
        sourceMap: !!process.env.SOURCE_MAP,
      },
    }],
  }),
  cssLoaderApp: extractCss0.extract({
    fallback: 'style-loader',
    use: [{
      loader: 'css-loader',
      options: {
        minimize: true,
        sourceMap: !!process.env.SOURCE_MAP,
      },
    }],
  }),

  minify,
  inject: false,

  // Utilize long-term caching by adding content hashes (not compilation hashes) to compiled assets
  output: {
    path: path.join(__dirname, '../../build'),
    filename: 'assets/[name].[chunkhash:8].js',
    chunkFilename: 'assets/[name].[chunkhash:8].chunk.js',
  },

  optimization: {
    concatenateModules: true,
    splitChunks: {
      minChunks: 4,
      name: false,
    },
    minimize: true,
    minimizer: [{
      apply: (compiler) => new UglifyJsPlugin({
        cache: true,
        parallel: true,
        sourceMap: !!process.env.SOURCE_MAP,
        uglifyOptions: {
          ecma: 8,
          compress: {
            // [#2842](https://github.com/mishoo/UglifyJS2/issues/2842)
            inline: 1,
          },
          output: {
            comments: false,
          },
        },
      }).apply(compiler),
    }],
  },

  plugins: [
    new GitRevisionPlugin(),
    extractCss0,
    extractCss1,
    new AdvancedInjectionPlugin({
      prefix: 'assets/',
      rules: [{
        match: 'app.html',
        head: [
          // app.vendor.css
          new AsyncCss(/^app\.vendor\..*\.css$/),
          // app.css
          new InlineCss(/^app\.(?!vendor).*\.css$/),
          // app.js common-app.chunk.js
          new Preload(/^(common-)?app\..*\.js$/, { as: 'script' }),
          // *.chunk.js
          new Prefetch(/\.chunk\.js$/),
          // *.worker.js
          new Prefetch(/^.*\.worker\.js$/),
        ],
        body: [
          // app.js
          new Js(/^app\..*\.js$/),
        ],
      }],
    }),
  ],

  devtool: process.env.SOURCE_MAP ? 'source-map' : undefined,

  performance: {
    assetFilter: (assetFilename) => !(/(\.map$)|(^(favicon\.))/.test(assetFilename)),
  },
});
