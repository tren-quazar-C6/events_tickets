FROM node:22-alpine
RUN apk add --no-cache cups-client
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
EXPOSE 9100
CMD ["node", "index.js"]