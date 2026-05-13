-- =====================================================
-- FIRESTORE ONLY COLLECTIONS SCHEMA
-- Project: Multiplayer-Study / Cloud Firestore (default)
-- Purpose: Tai lieu mo phong SQL cho cac collection CHI luu tren Firestore.
-- Note: Firestore khong chay file .sql nay truc tiep. File nay dung de xem
--       cau truc collection/document/field giong man hinh Firebase Console.
-- =====================================================

-- =====================================================
-- FIRESTORE DATABASE: (default)
-- ROOT COLLECTIONS:
--   1. users
--   2. playerData
--   3. gameModeProgress
--   4. levelProgress
--   5. playerShop
-- =====================================================

-- =====================================================
-- COLLECTION: users
-- DOCUMENT: users/{uid}
-- Mo ta: thong tin tai khoan Firebase Auth va profile nguoi choi.
-- Tao/cap nhat boi: FirebaseManager, AuthManager, SessionGuardService.
-- =====================================================
CREATE TABLE users (
  uid             VARCHAR(128) PRIMARY KEY,  -- Firebase Auth UID, cung la document id
  email           VARCHAR(255) NOT NULL,     -- email dang nhap
  characterName   VARCHAR(100) NOT NULL,     -- ten nhan vat, query unique khi dang ky
  grade           INT NOT NULL,              -- lop hoc: 1..5
  avatar          VARCHAR(255) DEFAULT '',   -- chuoi avatar cu/du phong
  avatarId        INT DEFAULT 0,             -- avatar dang chon
  createdAt       BIGINT NOT NULL,           -- Unix timestamp milliseconds
  lastLogin       BIGINT NOT NULL,           -- Unix timestamp milliseconds
  isActive        BOOLEAN DEFAULT TRUE,
  emailVerified   BOOLEAN DEFAULT FALSE,
  activeSessionId VARCHAR(128) DEFAULT NULL  -- chong dang nhap cung luc tren nhieu may
);

-- Firestore query dang dung:
-- users.whereEqualTo("characterName", value).limit(1)
CREATE INDEX idx_users_characterName ON users(characterName);

-- =====================================================
-- COLLECTION: playerData
-- DOCUMENT: playerData/{uid}
-- Mo ta: thong ke tong cua nguoi choi, dung cho profile va bang xep hang.
-- Tao/cap nhat boi: FirebaseManager, CloudSyncService, multiplayer result.
-- =====================================================
CREATE TABLE playerData (
  uid           VARCHAR(128) PRIMARY KEY,  -- document id
  characterName VARCHAR(100) NOT NULL,
  level         INT DEFAULT 1,
  totalXp       INT DEFAULT 0,
  totalScore    INT DEFAULT 0,
  rank          INT DEFAULT 0,             -- co the tinh tu totalScore
  coins         INT DEFAULT 0,
  gamesPlayed   INT DEFAULT 0,
  gamesWon      INT DEFAULT 0,
  winRate       FLOAT DEFAULT 0.0,
  lastUpdated   BIGINT NOT NULL,
  FOREIGN KEY (uid) REFERENCES users(uid)
);

-- Firestore query dang dung cho leaderboard:
-- playerData.orderBy("totalScore", descending).limit(...)
CREATE INDEX idx_playerData_totalScore ON playerData(totalScore DESC);

-- =====================================================
-- COLLECTION: gameModeProgress
-- DOCUMENT: gameModeProgress/{uid}_{gameMode}_{grade}
-- Vi du trong Firebase Console:
--   gameModeProgress/55nKozks..._chonda_1
-- Mo ta: tien do theo tung che do game va tung lop.
-- Moi user co 15 document ban dau: 3 gameMode x 5 grade.
-- =====================================================
CREATE TABLE gameModeProgress (
  progressId       VARCHAR(180) PRIMARY KEY, -- document id: {uid}_{gameMode}_{grade}
  uid              VARCHAR(128) NOT NULL,
  gameMode         VARCHAR(50) NOT NULL,     -- chonda | keothada | phithuyen
  grade            INT NOT NULL,             -- 1..5
  currentLevel     INT DEFAULT 1,
  maxLevelUnlocked INT DEFAULT 1,
  totalScore       INT DEFAULT 0,
  bestScore        INT DEFAULT 0,
  lastPlayed       BIGINT DEFAULT NULL,       -- null neu chua choi
  FOREIGN KEY (uid) REFERENCES users(uid)
);

CREATE INDEX idx_gameModeProgress_uid ON gameModeProgress(uid);
CREATE UNIQUE INDEX idx_gameModeProgress_unique
  ON gameModeProgress(uid, gameMode, grade);

-- =====================================================
-- COLLECTION: levelProgress
-- DOCUMENT: levelProgress/{uid}_{gameMode}_{grade}_{levelNumber}
-- Mo ta: diem cao nhat va so lan choi cua tung level da choi.
-- Chi tao document khi nguoi choi choi level do lan dau.
-- =====================================================
CREATE TABLE levelProgress (
  progressId  VARCHAR(200) PRIMARY KEY, -- document id: {uid}_{gameMode}_{grade}_{levelNumber}
  uid         VARCHAR(128) NOT NULL,
  gameMode    VARCHAR(50) NOT NULL,     -- chonda | keothada | phithuyen
  grade       INT NOT NULL,             -- 1..5
  levelNumber INT NOT NULL,             -- 1..100
  bestScore   INT DEFAULT 0,
  attempts    INT DEFAULT 0,
  FOREIGN KEY (uid) REFERENCES users(uid),
  FOREIGN KEY (uid, gameMode, grade) REFERENCES gameModeProgress(uid, gameMode, grade)
);

-- Firestore query dang dung:
-- levelProgress.whereEqualTo("uid", uid)
--              .whereEqualTo("gameMode", gameMode)
--              .whereEqualTo("grade", grade)
CREATE INDEX idx_levelProgress_by_mode_grade
  ON levelProgress(uid, gameMode, grade);

-- =====================================================
-- COLLECTION: playerShop
-- DOCUMENT: playerShop/{uid}_{shopType}
-- Mo ta: trang thai skin/item da mua va dang trang bi.
-- shopType:
--   chonda_skin
--   keothada_skin
--   keothada_phao
--   phithuyen_ship
-- =====================================================
CREATE TABLE playerShop (
  docId       VARCHAR(180) PRIMARY KEY, -- document id: {uid}_{shopType}
  uid         VARCHAR(128) NOT NULL,    -- lay tu docId de ve quan he
  shopType    VARCHAR(50) NOT NULL,     -- lay tu docId de ve quan he
  selectedId  INT DEFAULT 0,
  unlockedIds VARCHAR(255) DEFAULT '0', -- luu dang chuoi: "0,1,2"
  FOREIGN KEY (uid) REFERENCES users(uid),
  UNIQUE (uid, shopType)
);

-- =====================================================
-- RELATIONSHIPS (LOGICAL, FIRESTORE DOES NOT ENFORCE)
-- Tham chieu tu schema cu (game_toan_hoc.sql) va schema toi uu.
-- =====================================================
-- 1) users (1) ---- (1) playerData
--    users.uid == playerData.uid
--
-- 2) users (1) ---- (N) gameModeProgress
--    users.uid == gameModeProgress.uid
--    Moi user co 15 documents ban dau (3 gameMode x 5 grade).
--
-- 3) users (1) ---- (N) levelProgress
--    users.uid == levelProgress.uid
--    Chi tao khi choi level lan dau.
--
-- 4) users (1) ---- (N) playerShop
--    docId format: {uid}_{shopType}
--    1 user co 4 documents (4 shopType).
--
-- 5) gameModeProgress (1) ---- (N) levelProgress
--    Join logic: uid + gameMode + grade
--    Moi gameModeProgress co nhieu levelProgress (levelNumber 1..100).
--
-- 6) playerData (1) ---- (N) gameModeProgress / levelProgress (tong hop)
--    playerData la tong hop diem/coins/xp tu cac che do va tran dau.
--    Firestore chi luu gia tri tong, khong luu bang lich su.
--
-- =====================================================
-- JOIN KEYS / LINKING RULES (DE CO TRUY VAN)
-- =====================================================
-- A) users <-> playerData
--    JOIN: users.uid == playerData.uid
--
-- B) users <-> gameModeProgress
--    JOIN: users.uid == gameModeProgress.uid
--    progressId = {uid}_{gameMode}_{grade}
--
-- C) users <-> levelProgress
--    JOIN: users.uid == levelProgress.uid
--    progressId = {uid}_{gameMode}_{grade}_{levelNumber}
--
-- D) gameModeProgress <-> levelProgress
--    JOIN: uid + gameMode + grade
--    levelProgress.levelNumber thuoc [1..100]
--
-- E) users <-> playerShop
--    JOIN: docId prefix == {uid}_
--    docId = {uid}_{shopType}
--
-- F) playerData <-> users
--    characterName trong playerData dup voi users.characterName (de show profile nhanh).
--    Cap nhat dong bo khi doi ten (neu co).

-- =====================================================
-- DEFAULT DOCUMENTS WHEN REGISTERING A NEW USER
-- =====================================================

-- 1. users/{uid}
INSERT INTO users (
  uid, email, characterName, grade, avatar, avatarId,
  createdAt, lastLogin, isActive, emailVerified, activeSessionId
) VALUES (
  '{uid}', 'player@gmail.com', 'PlayerName', 1, '', 0,
  1714089600000, 1714089600000, TRUE, FALSE, NULL
);

-- 2. playerData/{uid}
INSERT INTO playerData (
  uid, characterName, level, totalXp, totalScore, rank,
  coins, gamesPlayed, gamesWon, winRate, lastUpdated
) VALUES (
  '{uid}', 'PlayerName', 1, 0, 0, 0,
  0, 0, 0, 0.0, 1714089600000
);

-- 3. gameModeProgress: tao 15 documents/user
INSERT INTO gameModeProgress (
  progressId, uid, gameMode, grade,
  currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed
) VALUES
('{uid}_chonda_1',    '{uid}', 'chonda',    1, 1, 1, 0, 0, NULL),
('{uid}_chonda_2',    '{uid}', 'chonda',    2, 1, 1, 0, 0, NULL),
('{uid}_chonda_3',    '{uid}', 'chonda',    3, 1, 1, 0, 0, NULL),
('{uid}_chonda_4',    '{uid}', 'chonda',    4, 1, 1, 0, 0, NULL),
('{uid}_chonda_5',    '{uid}', 'chonda',    5, 1, 1, 0, 0, NULL),
('{uid}_keothada_1',  '{uid}', 'keothada',  1, 1, 1, 0, 0, NULL),
('{uid}_keothada_2',  '{uid}', 'keothada',  2, 1, 1, 0, 0, NULL),
('{uid}_keothada_3',  '{uid}', 'keothada',  3, 1, 1, 0, 0, NULL),
('{uid}_keothada_4',  '{uid}', 'keothada',  4, 1, 1, 0, 0, NULL),
('{uid}_keothada_5',  '{uid}', 'keothada',  5, 1, 1, 0, 0, NULL),
('{uid}_phithuyen_1', '{uid}', 'phithuyen', 1, 1, 1, 0, 0, NULL),
('{uid}_phithuyen_2', '{uid}', 'phithuyen', 2, 1, 1, 0, 0, NULL),
('{uid}_phithuyen_3', '{uid}', 'phithuyen', 3, 1, 1, 0, 0, NULL),
('{uid}_phithuyen_4', '{uid}', 'phithuyen', 4, 1, 1, 0, 0, NULL),
('{uid}_phithuyen_5', '{uid}', 'phithuyen', 5, 1, 1, 0, 0, NULL);

-- 4. levelProgress khong tao luc dang ky.
-- Chi tao khi nguoi choi hoan thanh level:
INSERT INTO levelProgress (
  progressId, uid, gameMode, grade, levelNumber, bestScore, attempts
) VALUES (
  '{uid}_chonda_1_1', '{uid}', 'chonda', 1, 1, 950, 1
);

-- 5. playerShop tao/cap nhat khi mua, trang bi, restore hoac reset.
INSERT INTO playerShop (docId, uid, shopType, selectedId, unlockedIds) VALUES
('{uid}_chonda_skin',    '{uid}', 'chonda_skin',    0, '0'),
('{uid}_keothada_skin',  '{uid}', 'keothada_skin',  0, '0'),
('{uid}_keothada_phao',  '{uid}', 'keothada_phao',  0, '0'),
('{uid}_phithuyen_ship', '{uid}', 'phithuyen_ship', 0, '0');

-- =====================================================
-- EXAMPLE DOCUMENT LIKE FIREBASE CONSOLE IMAGE
-- =====================================================
INSERT INTO gameModeProgress (
  progressId, uid, gameMode, grade,
  currentLevel, maxLevelUnlocked, totalScore, bestScore, lastPlayed
) VALUES (
  '55nKozksIjhBfEaLNCinctdMFx43_chonda_1',
  '55nKozksIjhBfEaLNCinctdMFx43',
  'chonda',
  1,
  1,
  1,
  0,
  0,
  NULL
);

-- =====================================================
-- SUMMARY
-- =====================================================
-- Chi co 5 collection luu tren Firestore:
--   users/{uid}
--   playerData/{uid}
--   gameModeProgress/{uid}_{gameMode}_{grade}
--   levelProgress/{uid}_{gameMode}_{grade}_{levelNumber}
--   playerShop/{uid}_{shopType}
--
-- Khong dua vao file nay cac bang SQL cu/khong thay code ghi Firestore:
--   game_history
--   leaderboard_score
--   leaderboard_level
--   game_sessions
--   achievements
-- =====================================================
