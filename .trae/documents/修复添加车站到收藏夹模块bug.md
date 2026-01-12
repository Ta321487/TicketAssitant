## 问题分析
当只输入到达车站且条件为AND时，返回全部车票的bug，根本原因是搜索逻辑中缺少了到达站(arrive_station)的处理。

## 修复方案

### 1. 修改QueryFilterEventArgs类
- 添加ArriveStation属性，用于传递到达站筛选条件

### 2. 修改AdvancedQueryTicketViewModel类
- 添加到达站相关的属性（SelectedArriveStation、ArriveStationSearchText等）
- 添加到达站搜索功能
- 修改ApplyFilter方法，处理到达站条件

### 3. 修改数据库服务的搜索方法
- 在GetFilteredTrainRideInfoCountAsync方法中添加到达站筛选条件
- 在GetFilteredTrainRideInfosAsync方法中添加到达站筛选条件
- 确保到达站条件在AND/OR模式下都能正确处理

### 4. 修复逻辑
- 当只输入到达站且条件为AND时，应该只返回匹配到达站的车票
- 到达站筛选条件的处理逻辑应该与出发站保持一致
- 确保所有搜索条件都能正确组合

## 修复文件
- `d:\TicketAssist\TA_WPF\ViewModels\AdvancedQueryTicketViewModel.cs`
- `d:\TicketAssist\TA_WPF\Services\DatabaseService.cs`

## 预期效果
- 当只输入到达站且条件为AND时，只返回匹配到达站的车票
- 搜索逻辑中到达站的处理与出发站保持一致
- 不影响现有功能的正常使用