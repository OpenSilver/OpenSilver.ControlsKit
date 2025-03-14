﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Threading.Tasks;
using OpenSilver.WebAssembly;

namespace FastControls.TestApp.Browser.Pages
{
    [Route("/")]
    public class Index : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
        }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await Runner.RunApplicationAsync<TestApp.App>();
        }
    }
}