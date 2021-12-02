FROM node:lts AS builder

WORKDIR /code
COPY package.json package-lock.json /code/
RUN npm ci
COPY index-gui.html webpack.config.js /code/
COPY src /code/src/
ENV API_URL=/api
RUN npm run build

FROM nginx

COPY default.conf /etc/nginx/conf.d/default.conf
COPY art /var/www/art/
COPY --from=builder /code/dist /var/www/
