/* eslint-disable global-require */
const express = require('express');
const logger = require('./logger');

const argv = require('./argv');
const port = require('./port');

const app = express();

if (process.env.NODE_ENV === 'production') {
  require('./middlewares/addProdMiddlewares')(app);
} else {
  require('./middlewares/addDevMiddlewares')(app);
}

// get the intended host and port number, use localhost and port 3000 if not provided
const customHost = argv.host || process.env.HOST;
const host = customHost || null; // Let http.Server use its default IPv6/4 host
const prettyHost = customHost || 'localhost';

// Start your app.
app.listen(port, host, (err) => {
  if (err) {
    return logger.error(err.message);
  }

  logger.appStarted(port, prettyHost);
  return undefined;
});
