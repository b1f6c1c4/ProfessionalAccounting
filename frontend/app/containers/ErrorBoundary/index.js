import React from 'react';
import PropTypes from 'prop-types';

export class ErrorBoundary extends React.PureComponent {
  state = {
    error: null,
    info: null,
  };

  componentDidCatch(error, info) {
    // eslint-disable-next-line no-console
    console.error(error, info);
    this.setState({
      error: error.stack,
      info: info.componentStack,
    });
  }

  handleRetry = () => {
    window.localStorage.removeItem('accounting');
    window.location.reload();
  };

  render() {
    if (!this.state.error) return this.props.children;

    return (
      <div>
        <h1>糟糕！发生错误</h1>
        <p>您可以尝试刷新，或者点击“重置全部”按钮。</p>
        <input type="button" onClick={this.handleRetry} value="重置全部" />
        <hr />
        <pre>{this.state.error}</pre>
        <pre>{this.state.info}</pre>
      </div>
    );
  }
}

ErrorBoundary.propTypes = {
  children: PropTypes.any,
};

export default ErrorBoundary;
