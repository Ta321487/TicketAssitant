/*
 Navicat Premium Data Transfer

 Source Server         : localhost
 Source Server Type    : MySQL
 Source Server Version : 80037
 Source Host           : localhost:3306
 Source Schema         : db_hc

 Target Server Type    : MySQL
 Target Server Version : 80037
 File Encoding         : 65001

 Date: 24/03/2025 17:35:01
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for train_ride_info
-- ----------------------------
DROP TABLE IF EXISTS `train_ride_info`;
CREATE TABLE `train_ride_info`  (
  `id` int NOT NULL AUTO_INCREMENT COMMENT 'id',
  `ticket_number` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '取票号',
  `check_in_location` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '检票位置',
  `depart_station` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '出发车站',
  `train_no` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车次号',
  `arrive_station` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '到达车站',
  `depart_station_pinyin` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '出发车站拼音',
  `arrive_station_pinyin` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '到达车站拼音',
  `depart_date` date NULL DEFAULT NULL COMMENT '出发日期',
  `depart_time` time NULL DEFAULT NULL COMMENT '出发时间',
  `coach_no` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车厢号',
  `seat_no` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '座位号',
  `money` decimal(6, 2) NULL DEFAULT NULL COMMENT '金额',
  `seat_type` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '席别',
  `additional_info` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '附加信息（退票费/限乘当日当次车）',
  `ticket_purpose` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车票用途',
  `ticket_modification_type` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车票改签类型',
  `hint` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '提示信息',
  `ticket_type_flags` int NULL DEFAULT 0 COMMENT '票种类型（枚举）',
  `payment_channel_flags` int NULL DEFAULT 0 COMMENT '支付渠道（枚举）',
  `depart_station_code` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '出发车站代码',
  `arrive_station_code` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '到达车站代码',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `station_code`(`depart_station_code` ASC) USING BTREE,
  INDEX `arrive_station_code`(`arrive_station_code` ASC) USING BTREE,
  INDEX `fk_depart_station_pinyin`(`depart_station_pinyin` ASC) USING BTREE,
  INDEX `fk_arrive_station_pinyin`(`arrive_station_pinyin` ASC) USING BTREE,
  INDEX `idx_train_no`(`train_no` ASC, `depart_date` ASC) USING BTREE,
  INDEX `idx_depart_station`(`depart_station` ASC, `depart_date` ASC) USING BTREE,
  CONSTRAINT `fc_dc_arrive` FOREIGN KEY (`arrive_station_code`) REFERENCES `station_info` (`station_code`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fc_dp_arrive` FOREIGN KEY (`arrive_station_pinyin`) REFERENCES `station_info` (`station_pinyin`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fc_sp_depart` FOREIGN KEY (`depart_station_pinyin`) REFERENCES `station_info` (`station_pinyin`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_sc_depart` FOREIGN KEY (`depart_station_code`) REFERENCES `station_info` (`station_code`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE = InnoDB AUTO_INCREMENT = 187 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = DYNAMIC;

SET FOREIGN_KEY_CHECKS = 1;
