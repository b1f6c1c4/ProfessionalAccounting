import { render, rerender } from './app';

if (module.hot) {
  module.hot.accept(['./app'], () => {
    rerender();
  });
}

render();
