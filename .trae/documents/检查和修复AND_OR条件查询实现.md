# 修复AND/OR条件查询实现

## 问题分析

1. **用户的理解是正确的**：
   - AND应该是满足所有条件
   - OR是任意条件都行

2. **已修复的问题**：
   - 移除了DatabaseService.cs中不必要的IS NULL限制条件，确保当只输入到达车站时，且条件为AND时，系统会正确地只返回到达车站匹配的车票，而不是返回全部车票

3. **发现的新问题**：
   - 在QueryAllTicketsViewModel.cs中，HasAnyActiveFilter方法没有检查arriveStation，这会导致当用户只输入到达车站时，HasActiveFilters属性被错误地设置为false
   - 而AdvancedQueryTicketViewModel.cs中的HasAnyActiveFilter方法已经正确地检查了arriveStation
   - 这会导致UI显示错误，用户可能会看到"查询全部"而不是"查询"

4. **其他可能的问题**：
   - 我需要确保所有ViewModel中的HasAnyActiveFilter方法都正确检查了所有筛选条件
   - 我需要确保所有ViewModel中的GetCurrentFilterConditions方法都正确返回了所有筛选条件
   - 我需要确保所有ViewModel中的ApplyFilter方法都正确使用了所有筛选条件

## 修复计划

1. **修复QueryAllTicketsViewModel.cs中的HasAnyActiveFilter方法**：
   - 添加对arriveStation的检查，确保当只输入到达车站时，HasActiveFilters属性被正确设置为true
   - 参考AdvancedQueryTicketViewModel.cs中的实现

2. **检查其他ViewModel中的HasAnyActiveFilter方法**：
   - 确保AdvancedQueryRouteViewModel.cs、AdvancedQueryStationViewModel.cs等中的HasAnyActiveFilter方法也正确检查了所有筛选条件

3. **检查所有ViewModel中的GetCurrentFilterConditions方法**：
   - 确保它们都正确返回了所有筛选条件
   - 确保它们都包含arriveStation参数

4. **检查所有ViewModel中的ApplyFilter方法**：
   - 确保它们都正确使用了所有筛选条件
   - 确保它们都包含arriveStation参数

5. **测试修复结果**：
   - 编译项目，确保没有编译错误
   - 测试只输入到达车站，条件为AND的情况，确保HasActiveFilters属性被正确设置为true
   - 测试只输入到达车站，条件为AND的情况，确保只返回匹配的车票
   - 测试其他筛选条件组合，确保AND/OR逻辑都正确工作

## 预期结果

1. 当用户只输入到达车站，并且条件为AND时，HasActiveFilters属性被正确设置为true
2. 当用户只输入到达车站，并且条件为AND时，系统会正确地只返回到达车站匹配的车票
3. 当用户选择AND条件时，所有非空的筛选条件都必须满足
4. 当用户选择OR条件时，只要满足其中一个非空的筛选条件即可
5. UI显示正确，当有筛选条件时显示"查询"，当没有筛选条件时显示"查询全部"

## 实施步骤

1. 修复QueryAllTicketsViewModel.cs中的HasAnyActiveFilter方法
2. 检查其他ViewModel中的HasAnyActiveFilter方法
3. 检查所有ViewModel中的GetCurrentFilterConditions方法
4. 检查所有ViewModel中的ApplyFilter方法
5. 运行编译命令，确保没有编译错误
6. 测试修复结果