using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MinerControl.Utility
{
    // Adapted from examples here:
    // http://stackoverflow.com/questions/1315621/implementing-inotifypropertychanged-does-a-better-way-exist
    public abstract class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) 
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> selectorExpression)
        {
            if (selectorExpression == null)
                throw new ArgumentNullException("selectorExpression");
            var me = selectorExpression.Body as MemberExpression;

            // Nullable properties can be nested inside of a convert function
            if (me == null)
            {
                var ue = selectorExpression.Body as UnaryExpression;
                if (ue != null)
                {
                    me = ue.Operand as MemberExpression;
                }
            }

            if (me == null)
                throw new ArgumentException("The body must be a member expression");

            OnPropertyChanged(me.Member.Name);
        }

        protected void SetField<T>(ref T field, T value, Expression<Func<T>> selectorExpression, params Expression<Func<object>>[] additonal)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(selectorExpression);
            foreach (var item in additonal)
                OnPropertyChanged(item);
        }
    }
}
