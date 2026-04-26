-- =====================================================
-- FIRESTORE DATABASE SCHEMA - OPTIMIZED
-- Chỉ lưu dữ liệu QUAN TRỌNG để sync cross-device
-- =====================================================

-- =====================================================
-- TABLE: users
-- Thông tin tài khoản cơ bản
-- =====================================================
CREATE TABLE users (
  uid VARCHAR(128) PRIMARY KEY,
  email VARCHAR(255) NOT NULL UNIQUE,
  characterName VARCHAR(100) NOT NULL UNIQUE,
  age INT NOT NULL,
  createdAt BIGINT NOT NULL,
  lastLogin BIGINT NOT NULL
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_characterName ON users(characterName);

-- =====================================================
-- TABLE: playerData
-- Dữ liệu game chính - QUAN TRỌNG NHẤT
-- =====================================================
CREATE TABLE playerData (
  uid VARCHAR(128) PRIMARY KEY,
  characterName VARCHAR(100) NOT NULL,
  
  -- Thống kê tổng quan
  totalScore INT DEFAULT 0,
  totalXp INT DEFAULT 0,
  level INT DEFAULT 1,
  
  -- Thống kê trận đấu
  gamesPlayed INT DEFAULT 0,
  gamesWon INT DEFAULT 0,
  
  -- Timestamp
  lastUpdated BIGINT NOT NULL,
  
  FOREIGN KEY (uid) REFERENCES users(uid) ON DELETE CASCADE
);

CREATE INDEX idx_playerData_totalScore ON playerData(totalScore DESC);

-- =====================================================
-- TABLE: gameModeProgress
-- Tiến độ 3 chế độ game (MỚI - QUAN TRỌNG)
-- =====================================================
CREATE TABLE gameModeProgress (
  progressId VARCHAR(128) PRIMARY KEY,
  uid VARCHAR(128) NOT NULL,
  
  -- Chế độ game
  gameMode VARCHAR(50) NOT NULL,
  grade INT NOT NULL,
  
  -- Tiến độ
  currentLevel INT DEFAULT 1,
  maxLevelUnlocked INT DEFAULT 1,
  
  -- Thống kê
  totalScore INT DEFAULT 0,
  bestScore INT DEFAULT 0,
  
  -- Timestamp
  lastPlayed BIGINT,
  
  FOREIGN KEY (uid) REFERENCES users(uid) ON DELETE CASCADE,
  UNIQUE (uid, gameMode, grade)
);

CREATE INDEX idx_gameModeProgress_uid ON gameModeProgress(uid);

-- =====================================================
-- TABLE: levelProgress
-- Điểm từng level (để hiển thị màn hình chọn level)
-- =====================================================
CREATE TABLE levelProgress (
  progressId VARCHAR(128) PRIMARY KEY,
  uid VARCHAR(128) NOT NULL,
  
  -- Định danh level
  gameMode VARCHAR(50) NOT NULL,
  grade INT NOT NULL,
  levelNumber INT NOT NULL,
  
  -- Dữ liệu
  bestScore INT DEFAULT 0,
  attempts INT DEFAULT 0,
  
  FOREIGN KEY (uid) REFERENCES users(uid) ON DELETE CASCADE,
  UNIQUE (uid, gameMode, grade, levelNumber)
);

CREATE INDEX idx_levelProgress_uid ON levelProgress(uid);

-- =====================================================
-- GIÁ TRỊ MẶC ĐỊNH KHI ĐĂNG KÝ
-- =====================================================

-- 1. Tạo users
INSERT INTO users (uid, email, characterName, age, createdAt, lastLogin) 
VALUES ('abc123', 'player@gmail.com', 'MeoNinja', 10, 1714089600000, 1714089600000);

-- 2. Tạo playerData
INSERT INTO playerData (uid, characterName, totalScore, totalXp, level, gamesPlayed, gamesWon, lastUpdated) 
VALUES ('abc123', 'MeoNinja', 0, 0, 1, 0, 0, 1714089600000);

-- 3. Tạo gameModeProgress cho 3 chế độ x 5 khối lớp = 15 records
-- Chế độ 1: Chọn đáp án (chonda)
INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_chonda_1', 'abc123', 'chonda', 1, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_chonda_2', 'abc123', 'chonda', 2, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_chonda_3', 'abc123', 'chonda', 3, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_chonda_4', 'abc123', 'chonda', 4, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_chonda_5', 'abc123', 'chonda', 5, 1, 1, 0, 0, NULL);

-- Chế độ 2: Kéo thả (keothada)
INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_keothada_1', 'abc123', 'keothada', 1, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_keothada_2', 'abc123', 'keothada', 2, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_keothada_3', 'abc123', 'keothada', 3, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_keothada_4', 'abc123', 'keothada', 4, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_keothada_5', 'abc123', 'keothada', 5, 1, 1, 0, 0, NULL);

-- Chế độ 3: Phi thuyền (phithuyen)
INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_phithuyen_1', 'abc123', 'phithuyen', 1, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_phithuyen_2', 'abc123', 'phithuyen', 2, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_phithuyen_3', 'abc123', 'phithuyen', 3, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_phithuyen_4', 'abc123', 'phithuyen', 4, 1, 1, 0, 0, NULL);

INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) 
VALUES ('abc123_phithuyen_5', 'abc123', 'phithuyen', 5, 1, 1, 0, 0, NULL);

-- =====================================================
-- VÍ DỤ: Người chơi đã chơi đến level 10 lớp 1 chế độ chonda
-- =====================================================

-- Cập nhật gameModeProgress
UPDATE gameModeProgress 
SET currentLevel = 10, 
    maxLevelUnlocked = 11,
    totalScore = 9500,
    bestScore = 1000,
    lastPlayed = 1714089600000
WHERE progressId = 'abc123_chonda_1';

-- Lưu điểm từng level (1-10)
INSERT INTO levelProgress (progressId, uid, gameMode, grade, levelNumber, bestScore, attempts) VALUES
('abc123_chonda_1_1', 'abc123', 'chonda', 1, 1, 950, 1),
('abc123_chonda_1_2', 'abc123', 'chonda', 1, 2, 920, 2),
('abc123_chonda_1_3', 'abc123', 'chonda', 1, 3, 980, 1),
('abc123_chonda_1_4', 'abc123', 'chonda', 1, 4, 850, 3),
('abc123_chonda_1_5', 'abc123', 'chonda', 1, 5, 960, 1),
('abc123_chonda_1_6', 'abc123', 'chonda', 1, 6, 940, 2),
('abc123_chonda_1_7', 'abc123', 'chonda', 1, 7, 820, 4),
('abc123_chonda_1_8', 'abc123', 'chonda', 1, 8, 970, 1),
('abc123_chonda_1_9', 'abc123', 'chonda', 1, 9, 990, 1),
('abc123_chonda_1_10', 'abc123', 'chonda', 1, 10, 1000, 1);

-- =====================================================
-- TỔNG KẾT: CHỈ 4 TABLES QUAN TRỌNG
-- =====================================================

-- 1. users: Tài khoản (6 fields)
-- 2. playerData: Thống kê tổng (7 fields)
-- 3. gameModeProgress: Tiến độ 3 chế độ x 5 lớp (8 fields)
-- 4. levelProgress: Điểm từng level (7 fields)

-- ĐÃ XÓA:
-- - gameHistory (không cần lưu lịch sử chi tiết)
-- - gameSessions (multiplayer tạm thời, không cần lưu)
-- - achievements (tính tự động từ playerData)
-- - avatar, emailVerified, isActive (không quan trọng)
-- - winRate (tính từ gamesWon/gamesPlayed)
-- - rank (tính từ totalScore)
-- - stars (game không dùng hệ thống sao, chỉ tính điểm)
