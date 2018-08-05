const path = require('path');
const express = require('express');
const options = require('../../internals/webpack/webpack.prod');

module.exports = (app) => {
  const outputPath = path.join(__dirname, '../../build');
  const { publicPath } = options.output;

  app.use(publicPath, (req, res, next) => {
    req.headers['if-modified-since'] = undefined;
    req.headers['if-none-match'] = undefined;
    next();
  }, express.static(outputPath));

  app.get(`${publicPath}app/*`, (req, res) => res.sendFile(path.resolve(outputPath, 'app.html')));
};
