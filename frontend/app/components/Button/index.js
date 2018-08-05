import React from 'react';
import PropTypes from 'prop-types';

import {
  Button as RawButton,
} from '@material-ui/core';
import { Link } from 'react-router-dom';

class Button extends React.PureComponent {
  render() {
    const { component, ...other } = this.props;

    let comp = component;
    if (!comp && this.props.to) {
      comp = Link;
    }
    comp = comp || 'button';

    return (
      <RawButton
        component={comp}
        {...other}
      >
        {this.props.children}
      </RawButton>
    );
  }
}

Button.propTypes = {
  children: PropTypes.any,
  component: PropTypes.any,
  to: PropTypes.string,
};

export default Button;
