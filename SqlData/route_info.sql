SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for route_info
-- ----------------------------
DROP TABLE IF EXISTS `route_info`;
CREATE TABLE `route_info` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT '路线ID',
  `route_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '路线名称',
  `description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL COMMENT '路线描述',
  `cover_image` blob NULL COMMENT '封面图片',
  `create_time` datetime NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `update_time` datetime NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  `total_distance` decimal(10, 2) NULL DEFAULT 0 COMMENT '总里程(公里)',
  `is_favorite` tinyint(1) NULL DEFAULT 0 COMMENT '是否收藏',
  `sort_order` int NULL DEFAULT 0 COMMENT '排序顺序',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1; 