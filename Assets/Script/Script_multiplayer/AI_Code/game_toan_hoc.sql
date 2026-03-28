CREATE TABLE users (
  id VARCHAR(128) PRIMARY KEY,
  email VARCHAR(255) NOT NULL,
  username VARCHAR(100) NOT NULL,
  age INT NOT NULL,
  avatar VARCHAR(50),
  createdAt BIGINT NOT NULL,
  lastLogin BIGINT,
  isActive BOOLEAN DEFAULT 1
);

CREATE TABLE player_data (
  id VARCHAR(128) PRIMARY KEY,
  userId VARCHAR(128) NOT NULL,
  username VARCHAR(100) NOT NULL,
  totalScore INT DEFAULT 0,
  totalXp INT DEFAULT 0,
  currentLevel INT DEFAULT 1,
  rank INT DEFAULT 0,
  gamesPlayed INT DEFAULT 0,
  gamesWon INT DEFAULT 0,
  winRate FLOAT DEFAULT 0,
  FOREIGN KEY (userId) REFERENCES users(id)
);

CREATE TABLE level_progress (
  id VARCHAR(128) PRIMARY KEY,
  playerDataId VARCHAR(128) NOT NULL,
  levelNumber INT NOT NULL,
  maxScore INT DEFAULT 0,
  bestTime INT,
  attempts INT DEFAULT 0,
  completed BOOLEAN DEFAULT 0,
  xpEarned INT DEFAULT 0,
  FOREIGN KEY (playerDataId) REFERENCES player_data(id)
);

CREATE TABLE users (
  id VARCHAR(128) PRIMARY KEY,
  email VARCHAR(255) NOT NULL,
  username VARCHAR(100) NOT NULL,
  age INT NOT NULL,
  avatar VARCHAR(50),
  createdAt BIGINT NOT NULL,
  lastLogin BIGINT,
  isActive BOOLEAN DEFAULT 1
);

CREATE TABLE player_data (
  id VARCHAR(128) PRIMARY KEY,
  userId VARCHAR(128) NOT NULL,
  username VARCHAR(100) NOT NULL,
  totalScore INT DEFAULT 0,
  totalXp INT DEFAULT 0,
  currentLevel INT DEFAULT 1,
  rank INT DEFAULT 0,
  gamesPlayed INT DEFAULT 0,
  gamesWon INT DEFAULT 0,
  winRate FLOAT DEFAULT 0,
  FOREIGN KEY (userId) REFERENCES users(id)
);

CREATE TABLE level_progress (
  id VARCHAR(128) PRIMARY KEY,
  playerDataId VARCHAR(128) NOT NULL,
  levelNumber INT NOT NULL,
  maxScore INT DEFAULT 0,
  bestTime INT,
  attempts INT DEFAULT 0,
  completed BOOLEAN DEFAULT 0,
  xpEarned INT DEFAULT 0,
  FOREIGN KEY (playerDataId) REFERENCES player_data(id)
);

CREATE TABLE game_history (
  id VARCHAR(128) PRIMARY KEY,
  userId VARCHAR(128) NOT NULL,
  playerDataId VARCHAR(128) NOT NULL,
  username VARCHAR(100) NOT NULL,
  level INT NOT NULL,
  score INT NOT NULL,
  xpEarned INT,
  timeSpent INT,
  difficulty VARCHAR(20),
  timestamp BIGINT NOT NULL,
  gameMode VARCHAR(20),
  opponentId VARCHAR(128),
  result VARCHAR(10) NOT NULL,
  FOREIGN KEY (userId) REFERENCES users(id),
  FOREIGN KEY (playerDataId) REFERENCES player_data(id)
);

CREATE TABLE leaderboard_score (
  rank INT PRIMARY KEY,
  userId VARCHAR(128) NOT NULL,
  playerDataId VARCHAR(128) NOT NULL,
  username VARCHAR(100) NOT NULL,
  score INT NOT NULL,
  level INT,
  avatar VARCHAR(50),
  lastUpdated BIGINT NOT NULL,
  FOREIGN KEY (userId) REFERENCES users(id)
);

CREATE TABLE leaderboard_level (
  rank INT PRIMARY KEY,
  userId VARCHAR(128) NOT NULL,
  username VARCHAR(100) NOT NULL,
  currentLevel INT NOT NULL,
  score INT,
  lastUpdated BIGINT NOT NULL,
  FOREIGN KEY (userId) REFERENCES users(id)
);
