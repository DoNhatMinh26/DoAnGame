-- =====================================================
-- FIRESTORE DATABASE SCHEMA - UPDATED
-- Cập nhật: 2026 — Phản ánh đúng code hiện tại
-- File này dùng để tham khảo, KHÔNG chạy trực tiếp
-- Firestore tự tạo collections khi app chạy
-- =====================================================

-- =====================================================
-- COLLECTION: users/{uid}
-- Thông tin tài khoản — tạo khi đăng ký
-- =====================================================
CREATE TABLE users (
  uid           VARCHAR(128) PRIMARY KEY,   -- Firebase Auth UID
  email         VARCHAR(255) NOT NULL UNIQUE,
  characterName VARCHAR(100) NOT NULL UNIQUE, -- Tên nhân vật (unique check khi đăng ký)
  grade         INT          NOT NULL,        -- Lớp học 1–5 (thay thế age)
  avatar        VARCHAR(255) DEFAULT '',      -- Avatar ID (dự phòng cho tương lai)
  createdAt     BIGINT       NOT NULL,        -- Unix timestamp ms
  lastLogin     BIGINT       NOT NULL,        -- Unix timestamp ms
  isActive      BOOLEAN      DEFAULT TRUE,
  emailVerified BOOLEAN      DEFAULT FALSE
);

CREATE INDEX idx_users_email         ON users(email);
CREATE INDEX idx_users_characterName ON users(characterName);

-- =====================================================
-- COLLECTION: playerData/{uid}
-- Thống kê người chơi — tạo khi đăng ký, cập nhật liên tục
-- =====================================================
CREATE TABLE playerData (
  uid           VARCHAR(128) PRIMARY KEY,
  characterName VARCHAR(100) NOT NULL,

  -- Thống kê tổng quan
  totalScore    INT     DEFAULT 0,
  totalXp       INT     DEFAULT 0,   -- totalScore / 10
  level         INT     DEFAULT 1,   -- 1 + (totalXp / 100)
  rank          INT     DEFAULT 0,   -- tính từ totalScore (BXH)
  coins         INT     DEFAULT 0,   -- Tiền vàng (sync cross-device)

  -- Thống kê trận đấu multiplayer
  gamesPlayed   INT     DEFAULT 0,
  gamesWon      INT     DEFAULT 0,
  winRate       FLOAT   DEFAULT 0.0, -- gamesWon / gamesPlayed

  -- Timestamp
  lastUpdated   BIGINT  NOT NULL
);

-- Index cho BXH (sắp xếp theo điểm giảm dần)
CREATE INDEX idx_playerData_totalScore ON playerData(totalScore DESC);

-- =====================================================
-- COLLECTION: gameModeProgress/{uid}_{gameMode}_{grade}
-- Tiến độ 3 chế độ x 5 lớp = 15 records/user
-- Tạo khi đăng ký, cập nhật khi thắng màn
-- =====================================================
CREATE TABLE gameModeProgress (
  progressId       VARCHAR(128) PRIMARY KEY, -- format: {uid}_{gameMode}_{grade}
  uid              VARCHAR(128) NOT NULL,
  gameMode         VARCHAR(50)  NOT NULL,     -- "chonda" | "keothada" | "phithuyen"
  grade            INT          NOT NULL,     -- 1–5

  -- Tiến độ
  currentLevel     INT     DEFAULT 1,
  maxLevelUnlocked INT     DEFAULT 1,         -- Màn cao nhất đã mở khóa

  -- Thống kê chế độ này
  totalScore       INT     DEFAULT 0,
  bestScore        INT     DEFAULT 0,         -- Điểm cao nhất 1 màn

  -- Timestamp
  lastPlayed       BIGINT  DEFAULT NULL       -- NULL = chưa chơi
);

CREATE INDEX idx_gameModeProgress_uid ON gameModeProgress(uid);
CREATE UNIQUE INDEX idx_gameModeProgress_unique ON gameModeProgress(uid, gameMode, grade);

-- =====================================================
-- COLLECTION: levelProgress/{uid}_{gameMode}_{grade}_{level}
-- Điểm từng màn — tạo khi chơi lần đầu màn đó
-- =====================================================
CREATE TABLE levelProgress (
  progressId   VARCHAR(128) PRIMARY KEY, -- format: {uid}_{gameMode}_{grade}_{levelNumber}
  uid          VARCHAR(128) NOT NULL,
  gameMode     VARCHAR(50)  NOT NULL,
  grade        INT          NOT NULL,
  levelNumber  INT          NOT NULL,    -- 1–100

  bestScore    INT     DEFAULT 0,
  attempts     INT     DEFAULT 0         -- Số lần chơi màn này
);

CREATE INDEX idx_levelProgress_uid ON levelProgress(uid);
CREATE UNIQUE INDEX idx_levelProgress_unique ON levelProgress(uid, gameMode, grade, levelNumber);

-- =====================================================
-- COLLECTION: playerShop/{uid}_{shopType}
-- Trạng thái shop — tạo khi mua/trang bị skin lần đầu
-- shopType: "chonda_skin" | "keothada_skin" | "keothada_phao" | "phithuyen_ship"
-- =====================================================
CREATE TABLE playerShop (
  docId       VARCHAR(128) PRIMARY KEY, -- format: {uid}_{shopType}
  selectedId  INT     DEFAULT 0,        -- ID skin đang trang bị
  unlockedIds VARCHAR(255) DEFAULT '0'  -- Danh sách ID đã mua, dạng "0,1,2"
);

-- =====================================================
-- GIÁ TRỊ MẶC ĐỊNH KHI ĐĂNG KÝ TÀI KHOẢN MỚI
-- =====================================================

-- 1. users
INSERT INTO users (uid, email, characterName, grade, avatar, createdAt, lastLogin, isActive, emailVerified)
VALUES ('abc123', 'player@gmail.com', 'MeoNinja', 3, '', 1714089600000, 1714089600000, TRUE, FALSE);

-- 2. playerData
INSERT INTO playerData (uid, characterName, totalScore, totalXp, level, rank, coins, gamesPlayed, gamesWon, winRate, lastUpdated)
VALUES ('abc123', 'MeoNinja', 0, 0, 1, 0, 0, 0, 0, 0.0, 1714089600000);

-- 3. gameModeProgress — 15 records (3 chế độ x 5 lớp)
INSERT INTO gameModeProgress (progressId, uid, gameMode, grade, currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed) VALUES
('abc123_chonda_1',    'abc123', 'chonda',    1, 1, 1, 0, 0, NULL),
('abc123_chonda_2',    'abc123', 'chonda',    2, 1, 1, 0, 0, NULL),
('abc123_chonda_3',    'abc123', 'chonda',    3, 1, 1, 0, 0, NULL),
('abc123_chonda_4',    'abc123', 'chonda',    4, 1, 1, 0, 0, NULL),
('abc123_chonda_5',    'abc123', 'chonda',    5, 1, 1, 0, 0, NULL),
('abc123_keothada_1',  'abc123', 'keothada',  1, 1, 1, 0, 0, NULL),
('abc123_keothada_2',  'abc123', 'keothada',  2, 1, 1, 0, 0, NULL),
('abc123_keothada_3',  'abc123', 'keothada',  3, 1, 1, 0, 0, NULL),
('abc123_keothada_4',  'abc123', 'keothada',  4, 1, 1, 0, 0, NULL),
('abc123_keothada_5',  'abc123', 'keothada',  5, 1, 1, 0, 0, NULL),
('abc123_phithuyen_1', 'abc123', 'phithuyen', 1, 1, 1, 0, 0, NULL),
('abc123_phithuyen_2', 'abc123', 'phithuyen', 2, 1, 1, 0, 0, NULL),
('abc123_phithuyen_3', 'abc123', 'phithuyen', 3, 1, 1, 0, 0, NULL),
('abc123_phithuyen_4', 'abc123', 'phithuyen', 4, 1, 1, 0, 0, NULL),
('abc123_phithuyen_5', 'abc123', 'phithuyen', 5, 1, 1, 0, 0, NULL);

-- =====================================================
-- VÍ DỤ: Người chơi đã chơi đến màn 5 lớp 3 chế độ chonda
-- =====================================================

UPDATE gameModeProgress
SET currentLevel = 5, maxLevelUnlocked = 6, totalScore = 4750, bestScore = 1000, lastPlayed = 1714089600000
WHERE progressId = 'abc123_chonda_3';

INSERT INTO levelProgress (progressId, uid, gameMode, grade, levelNumber, bestScore, attempts) VALUES
('abc123_chonda_3_1', 'abc123', 'chonda', 3, 1, 950, 1),
('abc123_chonda_3_2', 'abc123', 'chonda', 3, 2, 920, 2),
('abc123_chonda_3_3', 'abc123', 'chonda', 3, 3, 980, 1),
('abc123_chonda_3_4', 'abc123', 'chonda', 3, 4, 900, 2),
('abc123_chonda_3_5', 'abc123', 'chonda', 3, 5, 1000, 1);

-- =====================================================
-- VÍ DỤ: Người chơi đã mua skin
-- =====================================================

INSERT INTO playerShop (docId, selectedId, unlockedIds) VALUES
('abc123_chonda_skin',    1, '0,1'),
('abc123_keothada_skin',  0, '0'),
('abc123_keothada_phao',  2, '0,1,2'),
('abc123_phithuyen_ship', 1, '0,1');

-- =====================================================
-- VÍ DỤ: Sau khi chơi multiplayer (5 trận, thắng 3)
-- =====================================================

UPDATE playerData
SET totalScore  = 1500,
    totalXp     = 150,
    level       = 2,
    coins       = 200,
    gamesPlayed = 5,
    gamesWon    = 3,
    winRate     = 0.6,
    lastUpdated = 1714089600000
WHERE uid = 'abc123';

-- =====================================================
-- CÔNG THỨC TÍNH LEVEL
-- =====================================================
-- totalXp   = totalScore / 10
-- level     = 1 + (totalXp / 100)
--
-- Ví dụ:
--   totalScore = 10000 → totalXp = 1000 → level = 11
--   totalScore = 5000  → totalXp = 500  → level = 6
--   totalScore = 1000  → totalXp = 100  → level = 2
--   totalScore = 0     → totalXp = 0    → level = 1

-- =====================================================
-- TỔNG KẾT: 5 COLLECTIONS
-- =====================================================
-- 1. users/{uid}                          — Tài khoản (9 fields)
-- 2. playerData/{uid}                     — Thống kê tổng (10 fields)
-- 3. gameModeProgress/{uid}_{mode}_{grade}— Tiến độ 15 records/user (9 fields)
-- 4. levelProgress/{uid}_{mode}_{grade}_{lvl} — Điểm từng màn (7 fields)
-- 5. playerShop/{uid}_{shopType}          — Shop skin (3 fields) [MỚI]

-- ĐÃ THAY ĐỔI SO VỚI PHIÊN BẢN CŨ:
-- + age → grade (lớp 1–5 thay tuổi)
-- + coins thêm vào playerData
-- + winRate thêm vào playerData
-- + playerShop collection mới (skin đã mua)
-- + avatar field dự phòng trong users
-- + gameModeProgress dùng cho single-player (không chỉ multiplayer)
