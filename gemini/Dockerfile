FROM node:22-alpine AS build
WORKDIR /app

COPY gemini/package*.json ./
RUN npm install

COPY gemini/ .

ARG REACT_APP_API_BASE_URL=/api
ENV REACT_APP_API_BASE_URL=$REACT_APP_API_BASE_URL

RUN npm run build

FROM nginx:1.27-alpine
COPY gemini/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/build /usr/share/nginx/html

EXPOSE 80
