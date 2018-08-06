import _ from 'lodash';
import React from 'react';
import PropTypes from 'prop-types';
import { compose } from 'redux';

import {
  withStyles,
  Divider,
  Drawer,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@material-ui/core';
import {
  Home,
  VideoLabel,
  CreditCard,
  MonetizationOn,
} from '@material-ui/icons';
import { Link } from 'react-router-dom';

// eslint-disable-next-line no-unused-vars
const styles = (theme) => ({
  drawer: {
    width: 300,
  },
  nested: {
    paddingLeft: theme.spacing.unit * 4,
  },
  item: {
    paddingLeft: 0,
  },
});

class GlobalDrawer extends React.PureComponent {
  handleProj = (name) => () => {
    this.props.onCloseDrawerAction();
    this.props.onPush(`/app/p/${name}`);
  };

  handleProfile = () => {
    this.props.onCloseDrawerAction();
    this.props.onPush('/app/');
  };

  handleUpload = () => {
    this.props.onCloseDrawerAction();
    this.props.onPush('/app/cashflow');
  };

  handleConsole = () => {
    this.props.onCloseDrawerAction();
    this.props.onPush('/app/console');
  };

  handleDownload = () => {
    this.props.onCloseDrawerAction();
    this.props.onPush('/app/download');
  };

  render() {
    const {
      classes,
      listProj,
    } = this.props;

    let projs;
    if (listProj) {
      projs = _.toPairs(listProj).map(([proj]) => {
        const content = (
          <Link to={`/app/p/${proj}`}>
            {proj}
          </Link>
        );
        return (
          <ListItem key={proj} button onClick={this.handleProj(proj)}>
            <ListItemText primary={content} />
          </ListItem>
        );
      });
    }

    return (
      <Drawer
        open={this.props.isDrawerOpen}
        onClose={this.props.onCloseDrawerAction}
      >
        <List
          component="nav"
          className={classes.drawer}
        >
          <ListItem button onClick={this.handleProfile}>
            <ListItemIcon>
              <Link to="/app/">
                <Home />
              </Link>
            </ListItemIcon>
            <ListItemText
              className={classes.item}
              primary={(
                <Link to="/app/">
                  摘要
                </Link>
              )}
            />
          </ListItem>
          <ListItem button onClick={this.handleBudget}>
            <ListItemIcon>
              <Link to="/app/budget">
                <MonetizationOn />
              </Link>
            </ListItemIcon>
            <ListItemText
              className={classes.item}
              primary={(
                <Link to="/app/budget">
                  预算
                </Link>
              )}
            />
          </ListItem>
          <ListItem button onClick={this.handleUpload}>
            <ListItemIcon>
              <Link to="/app/cashflow">
                <CreditCard />
              </Link>
            </ListItemIcon>
            <ListItemText
              className={classes.item}
              primary={(
                <Link to="/app/cashflow">
                  现金流
                </Link>
              )}
            />
          </ListItem>
          <ListItem button onClick={this.handleConsole}>
            <ListItemIcon>
              <Link to="/app/console">
                <VideoLabel />
              </Link>
            </ListItemIcon>
            <ListItemText
              className={classes.item}
              primary={(
                <Link to="/app/console">
                  控制台
                </Link>
              )}
            />
          </ListItem>
          <Divider />
          {projs}
        </List>
      </Drawer>
    );
  }
}

GlobalDrawer.propTypes = {
  classes: PropTypes.object.isRequired,
  onPush: PropTypes.func.isRequired,
  listProj: PropTypes.object,
  isDrawerOpen: PropTypes.bool.isRequired,
  onCloseDrawerAction: PropTypes.func.isRequired,
};

export default compose(
  withStyles(styles),
)(GlobalDrawer);
