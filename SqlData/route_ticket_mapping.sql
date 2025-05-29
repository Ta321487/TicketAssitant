SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for route_ticket_mapping
-- ----------------------------
DROP TABLE IF EXISTS `route_ticket_mapping`;
CREATE TABLE `route_ticket_mapping` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `route_id` int NOT NULL COMMENT '路线ID',
  `ticket_id` int NOT NULL COMMENT '车票ID',
  `order_index` int NULL DEFAULT 0 COMMENT '在路线中的顺序',
  `add_time` datetime NULL DEFAULT CURRENT_TIMESTAMP COMMENT '添加时间',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_route`(`route_id` ASC) USING BTREE,
  INDEX `idx_ticket`(`ticket_id` ASC) USING BTREE,
  CONSTRAINT `fk_rt_route` FOREIGN KEY (`route_id`) REFERENCES `route_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `fk_rt_ticket` FOREIGN KEY (`ticket_id`) REFERENCES `train_ride_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1; 