SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for route_statistics
-- ----------------------------
DROP TABLE IF EXISTS `route_statistics`;
CREATE TABLE `route_statistics` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT '统计ID',
  `route_id` int NOT NULL COMMENT '路线ID',
  `total_cost` decimal(10, 2) NULL DEFAULT 0 COMMENT '总花费',
  `total_distance` decimal(10, 2) NULL DEFAULT 0 COMMENT '总里程(公里)',
  `provinces_passed` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL COMMENT '经过的省份(逗号分隔)',
  `cities_passed` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL COMMENT '经过的城市(逗号分隔)',
  `seat_type_stats` json NULL COMMENT '不同席别的里程统计',
  `railway_bureau_stats` json NULL COMMENT '不同铁路局的统计',
  `update_time` datetime NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uk_route`(`route_id` ASC) USING BTREE,
  CONSTRAINT `fk_stat_route` FOREIGN KEY (`route_id`) REFERENCES `route_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1; 