using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace TA_WPF.Models
{
    /// <summary>
    /// 车票收藏夹信息模型
    /// </summary>
    public class TicketCollectionInfo : INotifyPropertyChanged
    {
        private int _id;
        private string _collectionName;
        private string _description;
        private byte[] _coverImage;
        private DateTime _createTime;
        private DateTime _updateTime;
        private int _sortOrder;
        private int _ticketCount;
        private bool _isSelected;
        private int _importance;

        /// <summary>
        /// 收藏夹ID
        /// </summary>
        public int Id 
        { 
            get => _id; 
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 收藏夹名称
        /// </summary>
        public string CollectionName 
        { 
            get => _collectionName; 
            set
            {
                if (_collectionName != value)
                {
                    _collectionName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 收藏夹描述
        /// </summary>
        public string Description 
        { 
            get => _description; 
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 封面图片 (Base64格式)
        /// </summary>
        public byte[] CoverImage 
        { 
            get => _coverImage; 
            set
            {
                if (_coverImage != value)
                {
                    _coverImage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime 
        { 
            get => _createTime; 
            set
            {
                if (_createTime != value)
                {
                    _createTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime 
        { 
            get => _updateTime; 
            set
            {
                if (_updateTime != value)
                {
                    _updateTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder 
        { 
            get => _sortOrder; 
            set
            {
                if (_sortOrder != value)
                {
                    _sortOrder = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 包含车票数量
        /// </summary>
        public int TicketCount
        {
            get => _ticketCount;
            set
            {
                if (_ticketCount != value)
                {
                    _ticketCount = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 重要性评分(1-5)
        /// </summary>
        public int Importance
        {
            get => _importance;
            set
            {
                if (_importance != value)
                {
                    _importance = value;
                    OnPropertyChanged();
                }
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 