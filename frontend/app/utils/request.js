import _ from 'lodash';
import axios from 'axios';

const apiUrl = (raw) => raw || '';

export const makeApi = (url, isWs, g = window) => {
  const api = apiUrl(process.env.API_URL);
  if (!isWs) {
    return api + url;
  }

  const protocol = _.get(g, 'location.protocol') === 'https:' ? 'wss:' : 'ws:';

  if (api.startsWith('//')) {
    return protocol + api + url;
  }

  if (api.startsWith('http:')) {
    return api.replace(/^http:/, 'ws:') + url;
  }

  if (api.startsWith('https:')) {
    return api.replace(/^https:/, 'wss:') + url;
  }

  const host = _.get(g, 'location.host');
  return `${protocol}//${host}${api}${url}`;
};

export const postProcess = (raw) => {
  if (!(raw instanceof Error)) {
    return raw.data;
  }

  const { message } = raw;

  throw _.assign(new Error(message), { raw });
};

// eslint-disable-next-line import/no-mutable-exports
let client;
/* istanbul ignore next */
if (process.env.NODE_ENV !== 'test') {
  /* istanbul ignore next */
  // eslint-disable-next-line global-require
  client = require('./request-core').default(makeApi);
}

export const getClient = /* istanbul ignore next */ (c) => {
  /* istanbul ignore next */
  if (process.env.NODE_ENV === 'test' && c) {
    /* istanbul ignore next */
    client = c;
  }
  /* istanbul ignore next */
  return client;
};

export const query = async (gql, vars) => {
  try {
    const response = await client.query({
      query: gql,
      variables: vars,
      fetchPolicy: 'network-only',
    });
    return postProcess(response);
  } catch (e) {
    return postProcess(e);
  }
};

export const mutate = async (gql, vars) => {
  try {
    const response = await client.mutate({
      mutation: gql,
      variables: vars,
      fetchPolicy: 'no-cache',
    });
    return postProcess(response);
  } catch (e) {
    return postProcess(e);
  }
};

export const subscribe = async (gql, vars) => {
  try {
    const response = await client.subscribe({
      query: gql,
      variables: vars,
      fetchPolicy: 'network-only',
    });
    return response;
  } catch (e) {
    return postProcess(e);
  }
};

const storageApi = axios.create({
  baseURL: makeApi('/storage'),
  headers: {
    Accept: 'application/json',
  },
});

export const listStorage = async (path) => {
  const { data } = await storageApi.get(`${path}/`);
  return data;
};

export const deleteStorage = async (path) => {
  const { data } = await storageApi.delete(path, {
    headers: {
      Accept: '*/*',
    },
  });
  return data;
};

export const uploadStorage = async (files) => {
  const form = new FormData();
  // eslint-disable-next-line no-restricted-syntax
  for (const f of files) {
    form.append('upload', f);
  }
  const { data } = await storageApi.post('upload/', form, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  return data;
};
