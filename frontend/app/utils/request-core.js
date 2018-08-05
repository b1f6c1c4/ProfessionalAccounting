import { ApolloClient } from 'apollo-client';
// eslint-disable-next-line import/no-extraneous-dependencies
import { split } from 'apollo-link';
import { HttpLink } from 'apollo-link-http';
import { WebSocketLink } from 'apollo-link-ws';
// eslint-disable-next-line import/no-extraneous-dependencies
import { getMainDefinition } from 'apollo-utilities';
import { InMemoryCache } from 'apollo-cache-inmemory';

export default (makeApi) => {
  const httpLink = new HttpLink({
    uri: makeApi('/graphql'),
  });

  const wsLink = new WebSocketLink({
    uri: makeApi('/subscriptions', true),
    options: {
      timeout: 50000,
      reconnect: true,
    },
  });

  const link = split(
    ({ query }) => {
      const { kind, operation } = getMainDefinition(query);
      return kind === 'OperationDefinition' && operation === 'subscription';
    },
    wsLink,
    httpLink,
  );

  return new ApolloClient({
    link,
    cache: new InMemoryCache(),
  });
};
