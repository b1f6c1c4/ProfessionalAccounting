FROM patrikjuvonen/docker-nginx-http3

COPY nginx.conf /etc/nginx/nginx.conf
COPY proxy.nginx.conf /etc/nginx/conf.d/
COPY h3.nginx.conf /etc/nginx/conf.d/
COPY art /var/www/
COPY public /var/www/public/
COPY errors /var/errors/
COPY dist archive.tar.xz /var/www/
COPY js /var/www/js/
