FROM accounting-frontend AS frontend

FROM nginx

COPY default.conf /etc/nginx/conf.d/default.conf

COPY --from=frontend / /var/www
