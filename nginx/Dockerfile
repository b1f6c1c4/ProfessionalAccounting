FROM patrikjuvonen/docker-nginx-http3

COPY nginx.conf /etc/nginx/nginx.conf
COPY h3.nginx.conf /etc/nginx/conf.d/h3.nginx.conf
COPY art /var/www/
COPY js /var/www/js/
COPY errors/*.html /var/errors/
COPY errors/index.css /var/www/errors.css
COPY dist archive.tar.xz /var/www/
