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
