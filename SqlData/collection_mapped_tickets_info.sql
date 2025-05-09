SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for collection_mapped_tickets_info
-- ----------------------------
DROP TABLE IF EXISTS `collection_mapped_tickets_info`;
CREATE TABLE `collection_mapped_tickets_info`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `collection_id` int NOT NULL COMMENT '收藏夹ID',
  `ticket_count` int NULL DEFAULT NULL COMMENT '包含车票数量',
  `ticket_id` int NOT NULL COMMENT '车票ID',
  `add_time` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  `importance` int NULL DEFAULT 0 COMMENT '评分1-5',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_collection`(`collection_id` ASC) USING BTREE,
  INDEX `idx_ticket`(`ticket_id` ASC) USING BTREE,
  CONSTRAINT `fk_ct_collection` FOREIGN KEY (`collection_id`) REFERENCES `ticket_collections_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `fk_ct_ticket` FOREIGN KEY (`ticket_id`) REFERENCES `train_ride_info` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1;
