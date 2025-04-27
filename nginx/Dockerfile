FROM nginx:stable-alpine

COPY nginx.conf /etc/nginx/nginx.conf
COPY proxy.nginx.conf /etc/nginx/conf.d/
COPY h3.nginx.conf /etc/nginx/conf.d/
COPY errors /var/errors/
COPY dist archive.tar.xz /var/www/

EXPOSE 18080
