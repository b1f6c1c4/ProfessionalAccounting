import React from 'react';
import PropTypes from 'prop-types';
import { compose } from 'redux';

import {
  withStyles,
} from '@material-ui/core';
import DocumentTitle from 'components/DocumentTitle';
import GlobalBar from 'components/GlobalBar';
import GlobalDrawer from 'components/GlobalDrawer';

// eslint-disable-next-line no-unused-vars
const styles = (theme) => ({
  root: {
    width: '100%',
  },
  wrapper: {
    marginTop: 70,
    marginLeft: 'auto',
    marginRight: 'auto',
  },
});

class GlobalPage extends React.PureComponent {
  render() {
    const {
      classes, // eslint-disable-line no-unused-vars
      onPush,
      listProj,
      isDrawerOpen,
      onOpenDrawerAction,
      onCloseDrawerAction,
    } = this.props;

    return (
      <div className={classes.root}>
        <DocumentTitle />
        <GlobalBar
          {...{
            onPush,
            isDrawerOpen,
            onOpenDrawerAction,
            onCloseDrawerAction,
          }}
        />
        <GlobalDrawer
          {...{
            onPush,
            listProj,
            isDrawerOpen,
            onCloseDrawerAction,
          }}
        />
        <div className={classes.wrapper}>
          {this.props.children}
        </div>
      </div>
    );
  }
}

GlobalPage.propTypes = {
  classes: PropTypes.object.isRequired,
  children: PropTypes.any,
  onPush: PropTypes.func.isRequired,
  listProj: PropTypes.object,
  isDrawerOpen: PropTypes.bool.isRequired,
  onOpenDrawerAction: PropTypes.func.isRequired,
  onCloseDrawerAction: PropTypes.func.isRequired,
};

export default compose(
  withStyles(styles),
)(GlobalPage);
