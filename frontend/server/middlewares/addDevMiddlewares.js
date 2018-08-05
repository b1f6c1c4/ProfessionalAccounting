const path = require('path');
const express = require('express');
const webpack = require('webpack');
const webpackDevMiddleware = require('webpack-dev-middleware');
const webpackHotMiddleware = require('webpack-hot-middleware');
const options = require('../../internals/webpack/webpack.dev');
const { dllPlugin } = require('../../package.json');

module.exports = (app) => {
  const compiler = webpack(options);
  const { output: { publicPath } } = options;
  const middleware = webpackDevMiddleware(compiler, {
    publicPath,
    stats: 'errors-only',
  });

  app.use(middleware);
  app.use(webpackHotMiddleware(compiler));

  if (dllPlugin) {
    const dllPath = path.join(__dirname, '../../', dllPlugin.path);
    app.use(`${publicPath}dll`, express.static(dllPath));
  }

  const fs = middleware.fileSystem;
  app.get(`${publicPath}app/*`, (req, res) => {
    // Avoid `path.join` here because it will convert / to \ on Windows,
    // which is incompatible with memory-fs
    fs.readFile('/tmp/ansys-moe/app.html', (err, file) => {
      if (err) {
        res.status(404).send();
      } else {
        res.send(file.toString());
      }
    });
  });
};
