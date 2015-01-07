#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class GradientStop : DependencyObject, IObservable
  {
    #region Protected fields

    protected AbstractProperty _colorProperty;
    protected AbstractProperty _offsetProperty;
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();

    protected SharpDX.Direct2D1.GradientStop _gradientStop2D;

    #endregion

    #region Ctor

    public GradientStop()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    public GradientStop(double offset, Color color)
    {
      Init();
      Color = color;
      Offset = offset;
      UpdateGradientStop2D();
      Attach();
    }

    void Init()
    {
      _colorProperty = new SProperty(typeof(Color), Color.White);
      _offsetProperty = new SProperty(typeof(double), 0.0);
      _gradientStop2D = new SharpDX.Direct2D1.GradientStop();
    }

    void Attach()
    {
      _colorProperty.Attach(OnPropertyChanged);
      _offsetProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _colorProperty.Detach(OnPropertyChanged);
      _offsetProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Detach();
      GradientStop s = (GradientStop) source;
      Color = s.Color;
      Offset = s.Offset;
      UpdateGradientStop2D();
      Attach();
    }

    #endregion

    public event ObjectChangedDlgt ObjectChanged
    {
      add { _objectChanged.Attach(value); }
      remove { _objectChanged.Detach(value); }
    }

    #region Protected methods

    protected void Fire()
    {
      _objectChanged.Fire(new object[] {this});
    }

    protected void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      Fire();
      UpdateGradientStop2D();
    }

    private void UpdateGradientStop2D()
    {
      _gradientStop2D.Color = Color;
      _gradientStop2D.Position = (float)Offset;
    }

    #endregion

    #region Public properties

    public SharpDX.Direct2D1.GradientStop GradientStop2D
    {
      get { return _gradientStop2D; }
    }

    public AbstractProperty ColorProperty
    {
      get { return _colorProperty; }
    }

    public Color Color
    {
      get { return (Color) _colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }

    public AbstractProperty OffsetProperty
    {
      get { return _offsetProperty; }
    }

    public double Offset
    {
      get { return (double) _offsetProperty.GetValue(); }
      set { _offsetProperty.SetValue(value); }
    }

    #endregion

    public override string ToString()
    {
      return Offset + ": " + Color.ToString();
    }
  }
}
