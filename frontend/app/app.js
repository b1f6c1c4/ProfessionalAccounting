import React from 'react';
import ReactDOM from 'react-dom';
import { Provider, connect } from 'react-redux';
import { ConnectedRouter } from 'react-router-redux';
import { createStructuredSelector, createSelector } from 'reselect';

import {
  createMuiTheme,
  CssBaseline,
  MuiThemeProvider,
} from '@material-ui/core';
import { lime, orange } from '@material-ui/core/colors';
import { Switch, Route } from 'react-router-dom';
import NotFoundPage from 'components/NotFoundPage';
import ErrorBoundary from 'containers/ErrorBoundary';
import GlobalContainer from 'containers/GlobalContainer';

import createHistory from 'history/createBrowserHistory';
import configureStore from 'utils/configureStore';

import './app.css';

// Create redux store with history
const initialState = {};
const history = createHistory();
const store = configureStore(initialState, history);
const MOUNT_NODE = document.getElementById('app');

const theme = createMuiTheme({
  typography: {
    fontFamily: '"Microsoft YaHei", sans-serif',
  },
  palette: {
    primary: {
      light: lime[100],
      main: lime[300],
      dark: lime[500],
      contrastText: '#000',
    },
    secondary: {
      light: orange[200],
      main: orange[400],
      dark: orange[600],
      contrastText: '#000',
    },
  },
});

const ConnectedSwitch = connect(createStructuredSelector({
  location: createSelector(
    (state) => state.getIn(['route', 'location']),
    (state) => state.toJS(),
  ),
}))(Switch);

export const render = () => {
  ReactDOM.render(
    <ErrorBoundary>
      <Provider store={store}>
        <ConnectedRouter history={history}>
          <React.Fragment>
            <CssBaseline />
            <MuiThemeProvider theme={theme}>
              <GlobalContainer>
                <ConnectedSwitch>
                  <Route component={NotFoundPage} />
                </ConnectedSwitch>
              </GlobalContainer>
            </MuiThemeProvider>
          </React.Fragment>
        </ConnectedRouter>
      </Provider>
    </ErrorBoundary>,
    MOUNT_NODE,
  );
};

export const rerender = () => {
  ReactDOM.unmountComponentAtNode(MOUNT_NODE);
  render();
};
