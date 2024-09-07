FROM node:alpine
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY package*.json ./

CMD ["node", "app/App.js"]
