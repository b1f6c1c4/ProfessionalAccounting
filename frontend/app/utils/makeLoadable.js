import Loadable from 'react-loadable';
import Loading from 'components/Loading';

export default (opts) => Loadable(Object.assign({
  loading: Loading,
  delay: 200,
}, opts));
