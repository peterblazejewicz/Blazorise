﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
#endregion

namespace Blazorise
{
    public abstract class BaseComponent : ComponentBase
    {
        #region Members

        private string elementId;

        private string customClass;

        private string customStyle;

        private IStyleProvider styleProvider;

        private IComponentMapper componentMapper;

        private Float @float = Float.None;

        private IFluentSpacing margin;

        private IFluentSpacing padding;

        private Visibility visibility = Visibility.Default;

        private Dictionary<string, object> parameters;

        /// <summary>
        /// A stack of functions to execute after the rendering.
        /// </summary>
        private Queue<Func<Task>> executeAfterRendereQueue;

        #endregion

        #region Constructors

        public BaseComponent()
        {
            ClassBuilder = new ClassBuilder( BuildClasses );
            StyleBuilder = new StyleBuilder( BuildStyles );
        }

        #endregion

        #region Methods

        protected void ExecuteAfterRender( Func<Task> action )
        {
            if ( executeAfterRendereQueue == null )
                executeAfterRendereQueue = new Queue<Func<Task>>();

            executeAfterRendereQueue.Enqueue( action );
        }

        protected override async Task OnAfterRenderAsync( bool firstRender )
        {
            // If the component has custom implementation we need to postpone the initialisation
            // until the custom component is rendered!
            if ( !HasCustomRegistration )
            {
                if ( firstRender )
                {
                    await OnFirstAfterRenderAsync();
                }

                if ( executeAfterRendereQueue?.Count > 0 )
                {
                    var actions = executeAfterRendereQueue.ToArray();
                    executeAfterRendereQueue.Clear();

                    foreach ( var action in actions )
                    {
                        await action();
                    }
                }
            }

            await base.OnAfterRenderAsync( firstRender );
        }

        protected virtual Task OnFirstAfterRenderAsync()
            => Task.CompletedTask;

        protected virtual void BuildClasses( ClassBuilder builder )
        {
            if ( Class != null )
                builder.Append( Class );

            if ( Margin != null )
                builder.Append( Margin.Class( ClassProvider ) );

            if ( Padding != null )
                builder.Append( Padding.Class( ClassProvider ) );

            if ( Float != Float.None )
                builder.Append( ClassProvider.ToFloat( Float ) );
        }

        protected virtual void BuildStyles( StyleBuilder builder )
        {
            if ( Style != null )
                builder.Append( Style );

            builder.Append( StyleProvider.Visibility( Visibility ) );
        }

        // use this until https://github.com/aspnet/Blazor/issues/1732 is fixed!!
        internal protected virtual void DirtyClasses()
        {
            ClassBuilder.Dirty();
        }

        protected virtual void DirtyStyles()
        {
            StyleBuilder.Dirty();
        }

        public override Task SetParametersAsync( ParameterView parameters )
        {
            if ( HasCustomRegistration )
            {
                // the component has a custom implementation so we need to copy the parameters for manual rendering
                this.parameters = new Dictionary<string, object>();

                foreach ( var parameter in parameters )
                {
                    if ( parameter.Cascading )
                        continue;

                    this.parameters.Add( parameter.Name, parameter.Value );
                }

                return base.SetParametersAsync( ParameterView.Empty );
            }
            else
                return base.SetParametersAsync( parameters );
        }

        /// <summary>
        /// Main method to render custom component implementation.
        /// </summary>
        /// <returns></returns>
        protected RenderFragment RenderCustomComponent() => builder =>
        {
            builder.OpenComponent( 0, ComponentMapper.GetImplementation( this ) );

            foreach ( var parameter in parameters )
            {
                builder.AddAttribute( 1, parameter.Key, parameter.Value );
            }

            builder.CloseComponent();
        };

        #endregion

        #region Properties

        /// <summary>
        /// Gets the reference to the rendered element.
        /// </summary>
        public ElementReference ElementRef { get; protected set; }

        /// <summary>
        /// Gets the unique id of the element.
        /// </summary>
        /// <remarks>
        /// Note that this ID is not defined for the component but instead for the underlined component that it represents.
        /// eg: for the TextEdit the ID will be set on the input element.
        /// </remarks>
        public string ElementId
        {
            get
            {
                // generate ID only on first use
                if ( elementId == null )
                    elementId = Utils.IDGenerator.Instance.Generate;

                return elementId;
            }
        }

        /// <summary>
        /// Gets the class builder.
        /// </summary>
        protected ClassBuilder ClassBuilder { get; private set; }

        /// <summary>
        /// Gets the built class-names based on all the rules set by the component parameters.
        /// </summary>
        protected string ClassNames => ClassBuilder.Class;

        /// <summary>
        /// Gets the style mapper.
        /// </summary>
        protected StyleBuilder StyleBuilder { get; private set; }

        /// <summary>
        /// Gets the built styles based on all the rules set by the component parameters.
        /// </summary>
        protected string StyleNames => StyleBuilder.Styles;

        /// <summary>
        /// Gets or sets the custom components mapper.
        /// </summary>
        [Inject]
        protected IComponentMapper ComponentMapper
        {
            get => componentMapper;
            set
            {
                componentMapper = value;

                HasCustomRegistration = componentMapper.HasRegistration( this );
            }
        }

        /// <summary>
        /// Indicates if components has a registered custom component.
        /// </summary>
        protected bool HasCustomRegistration { get; private set; }

        /// <summary>
        /// Gets or set the javascript runner.
        /// </summary>
        [Inject] protected IJSRunner JSRunner { get; set; }

        /// <summary>
        /// Gets or sets the classname provider.
        /// </summary>
        [Inject]
        protected IClassProvider ClassProvider { get; set; }

        /// <summary>
        /// Gets or sets the style provider.
        /// </summary>
        [Inject]
        protected IStyleProvider StyleProvider { get; set; }

        /// <summary>
        /// Custom css classname.
        /// </summary>
        [Parameter]
        public string Class
        {
            get => customClass;
            set
            {
                customClass = value;

                DirtyClasses();
            }
        }

        /// <summary>
        /// Custom html style.
        /// </summary>
        [Parameter]
        public string Style
        {
            get => customStyle;
            set
            {
                customStyle = value;

                DirtyStyles();
            }
        }

        /// <summary>
        /// Floats an element to the defined side.
        /// </summary>
        [Parameter]
        public Float Float
        {
            get => @float;
            set
            {
                @float = value;

                DirtyClasses();
            }
        }

        /// <summary>
        /// Defines the element margin spacing.
        /// </summary>
        [Parameter]
        public IFluentSpacing Margin
        {
            get => margin;
            set
            {
                margin = value;

                DirtyClasses();
            }
        }

        /// <summary>
        /// Defines the element padding spacing.
        /// </summary>
        [Parameter]
        public IFluentSpacing Padding
        {
            get => padding;
            set
            {
                padding = value;

                DirtyClasses();
            }
        }

        /// <summary>
        /// Gets or sets the element visibility.
        /// </summary>
        [Parameter]
        public Visibility Visibility
        {
            get => visibility;
            set
            {
                visibility = value;

                DirtyStyles();
            }
        }

        #endregion
    }
}
