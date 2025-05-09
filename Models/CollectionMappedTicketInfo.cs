using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TA_WPF.Models
{
    /// <summary>
    /// 收藏夹与车票的映射关系模型
    /// </summary>
    public class CollectionMappedTicketInfo : INotifyPropertyChanged
    {
        private int _id;
        private int _collectionId;
        private int _ticketId;
        private DateTime _addTime;
        private int _importance;

        /// <summary>
        /// 映射ID
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
        /// 收藏夹ID
        /// </summary>
        public int CollectionId
        {
            get => _collectionId;
            set
            {
                if (_collectionId != value)
                {
                    _collectionId = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 车票ID
        /// </summary>
        public int TicketId
        {
            get => _ticketId;
            set
            {
                if (_ticketId != value)
                {
                    _ticketId = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 添加时间
        /// </summary>
        public DateTime AddTime
        {
            get => _addTime;
            set
            {
                if (_addTime != value)
                {
                    _addTime = value;
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