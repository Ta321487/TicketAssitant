SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for route_station_mapping
-- ----------------------------
DROP TABLE IF EXISTS `route_station_mapping`;
CREATE TABLE `route_station_mapping` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `route_id` int NOT NULL COMMENT '路线ID',
  `station_id` int NOT NULL COMMENT '车站ID',
  `station_role` tinyint NULL DEFAULT 0 COMMENT '车站角色：1=起点,2=终点,4=经停,8=换乘',
  `stay_time` int NULL DEFAULT 0 COMMENT '计划停留时间(分钟)',
  `notes` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL COMMENT '备注',
  `add_time` datetime NULL DEFAULT CURRENT_TIMESTAMP COMMENT '添加时间',
		`distance_from_prev` decimal(10, 2) NULL DEFAULT 0.00 COMMENT '距离上一站点距离(公里)',
  `distance_from_start` decimal(10, 2) NULL DEFAULT 0.00 COMMENT '距起点累计距离(公里)',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_route`(`route_id` ASC) USING BTREE,
  INDEX `idx_station`(`station_id` ASC) USING BTREE,
  CONSTRAINT `fk_rs_route` FOREIGN KEY (`route_id`) REFERENCES `route_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `fk_rs_station` FOREIGN KEY (`station_id`) REFERENCES `station_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1; 