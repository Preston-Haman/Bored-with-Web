using Bored_with_Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bored_with_Web.Areas.Identity.Pages.Account.Manage
{
	public class GameReplayModel : PageModel
	{
		//This is for later.
		/*
		private readonly ApplicationDbContext _context;
		
		public GameReplayModel(ApplicationDbContext context)
		{
			_context = context;
		}
		
		public async Task<IActionResult> OnGetAsync()
        */
		public IActionResult OnGet()
		{
			//TODO: Grab replay information from the database when that's a thing...
			return Page();
		}
	}
}
