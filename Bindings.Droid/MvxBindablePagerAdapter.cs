// MvxBindablePagerAdapter.cs
// (c) Copyright Tomasz Cielecki http://ostebaronen.dk
// Licensed using Microsoft Public License (Ms-PL)
// Full License description can be found in the LICENSE
// file.
// 
// Project Lead - Tomasz Cielecli, @Cheesebaron, tomasz@ostebaronen.dk

using System.Collections;
using System.Collections.Specialized;
using Android.Content;
using Android.Support.V4.View;
using Android.Views;
using Android.Widget;
using Cirrious.MvvmCross.Binding;
using Cirrious.MvvmCross.Binding.Attributes;
using Cirrious.MvvmCross.Binding.Droid.Interfaces.Views;
using Cirrious.MvvmCross.Binding.Droid.Views;
using Cirrious.MvvmCross.Binding.ExtensionMethods;
using Cirrious.MvvmCross.Exceptions;
using Cirrious.MvvmCross.Interfaces.Platform.Diagnostics;

namespace Cheesebaron.MvvmCross.Bindings.Droid
{
    public class MvxBindablePagerAdapter
        : PagerAdapter
    {
        private readonly IMvxBindingActivity _bindingActivity;
        private readonly Context _context;
        private int _itemTemplateId;
        private IEnumerable _itemsSource;

        public MvxBindablePagerAdapter(Context context)
        {
            _context = context;
            _bindingActivity = context as IMvxBindingActivity;
            if (_bindingActivity == null)
                throw new MvxException(
                    "MvxBindableListView can only be used within a Context which supports IMvxBindingActivity");
            SimpleViewLayoutId = Android.Resource.Layout.SimpleListItem1;
        }

        protected Context Context
        {
            get { return _context; }
        }

        protected IMvxBindingActivity BindingActivity
        {
            get { return _bindingActivity; }
        }

        public int SimpleViewLayoutId { get; set; }

        [MvxSetToNullAfterBinding]
        public IEnumerable ItemsSource
        {
            get { return _itemsSource; }
            set { SetItemsSource(value); }
        }

        public int ItemTemplateId
        {
            get { return _itemTemplateId; }
            set
            {
                if (_itemTemplateId == value)
                    return;
                _itemTemplateId = value;

                // since the template has changed then let's force the list to redisplay by firing NotifyDataSetChanged()
                if (_itemsSource != null)
                    NotifyDataSetChanged();
            }
        }

        public override int Count
        {
            get { return _itemsSource.Count(); }
        }

        protected virtual void SetItemsSource(IEnumerable value)
        {
            if (_itemsSource == value)
                return;
            var existingObservable = _itemsSource as INotifyCollectionChanged;
            if (existingObservable != null)
                existingObservable.CollectionChanged -= OnItemsSourceCollectionChanged;
            _itemsSource = value;
            if (_itemsSource != null && !(_itemsSource is IList))
                MvxBindingTrace.Trace(MvxTraceLevel.Warning,
                                      "Binding to IEnumerable rather than IList - this can be inefficient, especially for large lists");
            var newObservable = _itemsSource as INotifyCollectionChanged;
            if (newObservable != null)
                newObservable.CollectionChanged += OnItemsSourceCollectionChanged;
            NotifyDataSetChanged();
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyDataSetChanged(e);
        }

        public virtual void NotifyDataSetChanged(NotifyCollectionChangedEventArgs e)
        {
            base.NotifyDataSetChanged();
        }

        public int GetPosition(object item)
        {
            return _itemsSource.GetPosition(item);
        }

        public System.Object GetRawItem(int position)
        {
            return _itemsSource.ElementAt(position);
        }

        private View GetView(int position, View convertView, ViewGroup parent, int templateId)
        {
            if (_itemsSource == null)
            {
                MvxBindingTrace.Trace(MvxTraceLevel.Error, "GetView called when ItemsSource is null");
                return null;
            }

            // 15 Oct 2012 - this position check removed as it's inefficient, especially for IEnumerable based collections
            /*
            if (position < 0 || position >= Count)
            {
                MvxBindingTrace.Trace(MvxTraceLevel.Error, "GetView called with invalid Position - zero indexed {0} out of {1}", position, Count);
                return null;
            }
            */

            var source = GetRawItem(position);

            return GetBindableView(convertView, source, templateId);
        }

        protected virtual View GetSimpleView(View convertView, object source)
        {
            if (convertView == null)
            {
                convertView = CreateSimpleView(source);
            }
            else
            {
                BindSimpleView(convertView, source);
            }

            return convertView;
        }

        protected virtual void BindSimpleView(View convertView, object source)
        {
            var textView = convertView as TextView;
            if (textView != null)
            {
                textView.Text = (source ?? "").ToString();
            }
        }

        protected virtual View CreateSimpleView(object source)
        {
            var view = _bindingActivity.NonBindingInflate(SimpleViewLayoutId, null);
            BindSimpleView(view, source);
            return view;
        }

        protected virtual View GetBindableView(View convertView, object source)
        {
            return GetBindableView(convertView, source, ItemTemplateId);
        }

        protected virtual View GetBindableView(View convertView, object source, int templateId)
        {
            if (templateId == 0)
            {
                // no template seen - so use a standard string view from Android and use ToString()
                return GetSimpleView(convertView, source);
            }

            // we have a templateid so lets use bind and inflate on it :)
            var viewToUse = convertView as IMvxBindableListItemView;
            if (viewToUse != null)
                if (viewToUse.TemplateId != templateId)
                    viewToUse = null;

            if (viewToUse == null)
                viewToUse = CreateBindableView(source, templateId);
            else
                BindBindableView(source, viewToUse);

            return viewToUse as View;
        }

        protected virtual void BindBindableView(object source, IMvxBindableListItemView viewToUse)
        {
            viewToUse.BindTo(source);
        }

        protected virtual MvxBindableListItemView CreateBindableView(object source, int templateId)
        {
            return new MvxBindableListItemView(_context, _bindingActivity, templateId, source);
        }

        public override Java.Lang.Object InstantiateItem(ViewGroup container, int position)
        {
            var view = GetView(position, null, container, ItemTemplateId > 0 ? ItemTemplateId : 0);

            container.AddView(view);

            return view;
        }

        public override void DestroyItem(ViewGroup container, int position, Java.Lang.Object obj)
        {
            var view = (View)obj;
            container.RemoveView(view);
            view.Dispose();
        }

        public override bool IsViewFromObject(View p0, Java.Lang.Object p1)
        {
            return p0 == p1;
        }
    }
}