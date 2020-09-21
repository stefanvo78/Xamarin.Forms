using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Xamarin.Forms.Internals;
using Xamarin.Platform;

namespace Xamarin.Forms
{
	public class View : VisualElement, IView, IViewController, IGestureController, IGestureRecognizers, IPropertyMapperView
	{
		SizeRequest _desiredSize;
		bool _isMeasureValid;
		bool _isArrangeValid;

		protected internal IGestureController GestureController => this;

		public static readonly BindableProperty VerticalOptionsProperty =
			BindableProperty.Create(nameof(VerticalOptions), typeof(LayoutOptions), typeof(View), LayoutOptions.Fill,
									propertyChanged: (bindable, oldvalue, newvalue) =>
									((View)bindable).InvalidateMeasureInternal(InvalidationTrigger.VerticalOptionsChanged));

		public static readonly BindableProperty HorizontalOptionsProperty =
			BindableProperty.Create(nameof(HorizontalOptions), typeof(LayoutOptions), typeof(View), LayoutOptions.Fill,
									propertyChanged: (bindable, oldvalue, newvalue) =>
									((View)bindable).InvalidateMeasureInternal(InvalidationTrigger.HorizontalOptionsChanged));

		public static readonly BindableProperty MarginProperty =
			BindableProperty.Create(nameof(Margin), typeof(Thickness), typeof(View), default(Thickness),
									propertyChanged: MarginPropertyChanged);

		internal static readonly BindableProperty MarginLeftProperty =
			BindableProperty.Create("MarginLeft", typeof(double), typeof(View), default(double),
									propertyChanged: OnMarginLeftPropertyChanged);

		static void OnMarginLeftPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var margin = (Thickness)bindable.GetValue(MarginProperty);
			margin.Left = (double)newValue;
			bindable.SetValue(MarginProperty, margin);
		}

		internal static readonly BindableProperty MarginTopProperty =
			BindableProperty.Create("MarginTop", typeof(double), typeof(View), default(double),
									propertyChanged: OnMarginTopPropertyChanged);

		static void OnMarginTopPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var margin = (Thickness)bindable.GetValue(MarginProperty);
			margin.Top = (double)newValue;
			bindable.SetValue(MarginProperty, margin);
		}

		internal static readonly BindableProperty MarginRightProperty =
			BindableProperty.Create("MarginRight", typeof(double), typeof(View), default(double),
									propertyChanged: OnMarginRightPropertyChanged);

		static void OnMarginRightPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var margin = (Thickness)bindable.GetValue(MarginProperty);
			margin.Right = (double)newValue;
			bindable.SetValue(MarginProperty, margin);
		}

		internal static readonly BindableProperty MarginBottomProperty =
			BindableProperty.Create("MarginBottom", typeof(double), typeof(View), default(double),
									propertyChanged: OnMarginBottomPropertyChanged);


		static void OnMarginBottomPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var margin = (Thickness)bindable.GetValue(MarginProperty);
			margin.Bottom = (double)newValue;
			bindable.SetValue(MarginProperty, margin);
		}

		readonly ObservableCollection<IGestureRecognizer> _gestureRecognizers = new ObservableCollection<IGestureRecognizer>();

		protected internal View()
		{
			_gestureRecognizers.CollectionChanged += (sender, args) =>
			{
				void AddItems()
				{
					foreach (IElement item in args.NewItems.OfType<IElement>())
					{
						ValidateGesture(item as IGestureRecognizer);
						item.Parent = this;
						GestureController.CompositeGestureRecognizers.Add(item as IGestureRecognizer);
					}
				}

				void RemoveItems()
				{
					foreach (IElement item in args.OldItems.OfType<IElement>())
					{
						item.Parent = null;
						GestureController.CompositeGestureRecognizers.Remove(item as IGestureRecognizer);
					}
				}

				switch (args.Action)
				{
					case NotifyCollectionChangedAction.Add:
						AddItems();
						break;
					case NotifyCollectionChangedAction.Remove:
						RemoveItems();
						break;
					case NotifyCollectionChangedAction.Replace:
						AddItems();
						RemoveItems();
						break;
					case NotifyCollectionChangedAction.Reset:
						foreach (IElement item in _gestureRecognizers.OfType<IElement>())
							item.Parent = this;
						foreach (IElement item in GestureController.CompositeGestureRecognizers.OfType<IElement>())
							item.Parent = this;
						break;
				}
			};
		}

		public IList<IGestureRecognizer> GestureRecognizers
		{
			get { return _gestureRecognizers; }
		}

		ObservableCollection<IGestureRecognizer> _compositeGestureRecognizers;

		IList<IGestureRecognizer> IGestureController.CompositeGestureRecognizers
		{
			get { return _compositeGestureRecognizers ?? (_compositeGestureRecognizers = new ObservableCollection<IGestureRecognizer>()); }
		}

		public virtual IList<GestureElement> GetChildElements(Point point)
		{
			return null;
		}

		public LayoutOptions HorizontalOptions
		{
			get { return (LayoutOptions)GetValue(HorizontalOptionsProperty); }
			set { SetValue(HorizontalOptionsProperty, value); }
		}

		public Thickness Margin
		{
			get { return (Thickness)GetValue(MarginProperty); }
			set { SetValue(MarginProperty, value); }
		}

		public LayoutOptions VerticalOptions
		{
			get { return (LayoutOptions)GetValue(VerticalOptionsProperty); }
			set { SetValue(VerticalOptionsProperty, value); }
		}

		protected override void OnBindingContextChanged()
		{
			this.PropagateBindingContext(GestureRecognizers);
			base.OnBindingContextChanged();
		}

		static void MarginPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			((View)bindable).InvalidateMeasureInternal(InvalidationTrigger.MarginChanged);
		}

		void ValidateGesture(IGestureRecognizer gesture)
		{
			if (gesture == null)
				return;
			if (gesture is PinchGestureRecognizer && _gestureRecognizers.GetGesturesFor<PinchGestureRecognizer>().Count() > 1)
				throw new InvalidOperationException($"Only one {nameof(PinchGestureRecognizer)} per view is allowed");
		}

		#region IView

		Rectangle IFrameworkElement.Frame => Bounds;

		protected IViewHandler Handler { get; set; }

		IViewHandler IFrameworkElement.Handler
		{
			get
			{
				return Handler;
			}

			set
			{
				Handler = value;
			}
		}

		protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			base.OnPropertyChanged(propertyName);
			(Handler)?.UpdateValue(propertyName);
		}

		IFrameworkElement IFrameworkElement.Parent => Parent as IView;

		SizeRequest IFrameworkElement.DesiredSize => _desiredSize;

		bool IFrameworkElement.IsMeasureValid => _isMeasureValid;

		bool IFrameworkElement.IsArrangeValid => _isArrangeValid;


		void IFrameworkElement.Arrange(Rectangle bounds)
		{
			if (_isArrangeValid)
				return;
			_isArrangeValid = true;
			Layout(bounds);
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);
			Handler?.SetFrame(Bounds);
		}

		SizeRequest IFrameworkElement.Measure(double widthConstraint, double heightConstraint)
		{
			if (!_isMeasureValid)
			{
				// TODO ezhart Adjust constraints to account for margins

				// TODO ezhart If we can find reason to, we may need to add a MeasureFlags parameter to IFrameworkElement.Measure
				// Forms has and (very occasionally) uses one. I'd rather not muddle this up with it, but if it's necessary
				// we can add it. The default is MeasureFlags.None, but nearly every use of it is MeasureFlags.IncludeMargins,
				// so it's an awkward default. 

				// I'd much rather just get rid of all the uses of it which don't include the margins, and have "with margins"
				// be the default. It's more intuitive and less code to write. Also, I sort of suspect that the uses which
				// _don't_ include the margins are actually bugs.
				_desiredSize = Handler.GetDesiredSize(widthConstraint, heightConstraint);// this.OnMeasure(widthConstraint, heightConstraint);
			}
				
			_isMeasureValid = true;
			return _desiredSize;
		}

		void IFrameworkElement.InvalidateMeasure()
		{
			_isMeasureValid = false;
			_isArrangeValid = false;
			this.InvalidateMeasure();
		}

		void IFrameworkElement.InvalidateArrange()
		{
			_isArrangeValid = false;
		}

		protected PropertyMapper propertyMapper;

		protected PropertyMapper<T> GetRendererOverides<T>() where T : IView => (PropertyMapper<T>)(propertyMapper as PropertyMapper<T> ?? (propertyMapper = new PropertyMapper<T>()));
		PropertyMapper IPropertyMapperView.GetPropertyMapperOverrides() => propertyMapper;


		#endregion
	}
}