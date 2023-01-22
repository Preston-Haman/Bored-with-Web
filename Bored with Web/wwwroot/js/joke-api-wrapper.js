
window.addEventListener("load", function () {
	//Load the Joke API information by making a request to https://v2.jokeapi.dev/info
	const JOKE_API_INFO_URL = "https://v2.jokeapi.dev/info";

	jokeGetXHR(JOKE_API_INFO_URL,
		//callback
		function (response) {
			//Gonna log the version of the API for kicks.
			console.log(`Joke API Version: ${response.version}`);

			//We want to know what joke categories exist, as well as what filtering flags exist.
			let categories = response.jokes.categories; //This is an array of strings!
			let flags = response.jokes.flags; //This is an array of strings, as well!

			//Now that we have the above information, we should load it onto the page.
			//We'll start with a helper method to create checkboxes and labels.
			let createCheckboxAndLabel = function (parentElement, idPrefix, labelText) {
				let checkbox = document.createElement("input");
				checkbox.setAttribute("id", `joke-api-${idPrefix}-${labelText}`);
				checkbox.setAttribute("type", "checkbox");
				checkbox.setAttribute("data-joke-setting", labelText);

				let label = document.createElement("label");
				label.setAttribute("for", `joke-api-${idPrefix}-${labelText}`);
				label.innerText = labelText;

				parentElement.appendChild(checkbox);
				parentElement.appendChild(label);
			}

			categories.forEach(function (category) {
				//Do nothing for the "Any" category; no reason to include it with our checkboxes.
				if (category == "Any") return;

				let categoryDiv = document.getElementById("joke-api-categories");
				createCheckboxAndLabel(categoryDiv, "category", category);
			});

			flags.forEach(function (flag) {
				let flagDiv = document.getElementById("joke-api-filter");
				createCheckboxAndLabel(flagDiv, "flag", flag);
			});

			//Immediately populate the first joke if the API is available.
			getJoke();
		},

		//errorHandler
		function (err, msg) {
			console.error(msg, err);
			document.getElementById("joke-api").hidden = true;
		}
	);

	document.getElementById("joke-api-get-joke-btn").onclick = getJoke;
});

/**
 * Retrieves a joke from the Joke API and displays it to the user.
 */
function getJoke() {
	//Before we can get a joke, we have to build the URL.
	//The URL contains settings, so we'll have to get those from the user.
	//The basic URL is https://v2.jokeapi.dev/joke/[Categories] where Categories is a list separated by commas, plus signs, or minus signs.
	//The categories have been populated onto the page in a special div as inputs, with the exception of the "Any" category.

	//Helper method to build url segments based on input elements on the page
	let buildUrlSegmentFromCheckboxInputList = function (inputList, separator) {
		let urlSegment = "";

		inputList.forEach(function (input) {
			if (input.checked) {
				let segmentMember = input.getAttribute("data-joke-setting");

				//If it's not empty
				if (urlSegment) {
					urlSegment += `${separator}${segmentMember}`;
				} else {
					urlSegment = segmentMember;
				}
			}
		});

		return urlSegment;
	};

	let categories = document.getElementById("joke-api-categories").querySelectorAll("input");
	let categoryUrlSegment = buildUrlSegmentFromCheckboxInputList(categories, "-");

	//If it's still an empty string, use "Any"
	if (!categoryUrlSegment) {
		categoryUrlSegment = "Any";
	}

	//We also expose blacklist flags to the user; so we need to get those, too.
	//Those are added as a query string: ?blacklistFlags=flag1[,flag2,...]
	let flags = document.getElementById("joke-api-filter").querySelectorAll("input");
	let flagUrlSegment = buildUrlSegmentFromCheckboxInputList(flags, ",");

	//If it's not empty, prepend the query
	if (flagUrlSegment) {
		flagUrlSegment = `&blacklistFlags=${flagUrlSegment}`;
	}

	//The other setting we expose to the user is safe-mode.
	let safeModeUrlSegment = "";
	if (document.getElementById("joke-api-safe-mode").checked) {
		safeModeUrlSegment = "&safe-mode";
	}

	let jokeAPIUrl = `https://v2.jokeapi.dev/joke/${categoryUrlSegment}?lang=en${flagUrlSegment}${safeModeUrlSegment}`;

	jokeGetXHR(jokeAPIUrl,
		//callback
		function (response) {
			//Clear the old joke...
			document.getElementById("joke-api-joke-setup").innerText = "";
			document.getElementById("joke-api-joke-delivery").innerText = "";

			//Populate the new one.
			if (response.type == "twopart") {
				document.getElementById("joke-api-joke-setup").innerText = response.setup;
				document.getElementById("joke-api-joke-delivery").innerText = response.delivery;
			} else if (response.type == "single") {
				document.getElementById("joke-api-joke-setup").innerText = response.joke;
			}
		},

		//errorHandler
		function (err, msg) {
			//Clear the old joke...
			document.getElementById("joke-api-joke-setup").innerText = "";
			document.getElementById("joke-api-joke-delivery").innerText = "";

			//Let the user know something went wrong...
			document.getElementById("joke-api-joke-delivery").innerText = "Something went wrong! We were unable to retrieve a joke for you."
			console.log(msg, err);
		}
	);
}

/**
 * Handles making XHR GET requests for the Joke API.
 * 
 * @param {String} url - The url to send the request to.
 * @param {Function} callback - A function accepting an object argument that represents the JSON response from the Joke API.
 * @param {Function} errorHandler - A function accepting an error argument, and an optional message argument.
 */
function jokeGetXHR(url, callback, errorHandler) {
	try {
		let xhr = new XMLHttpRequest();
		xhr.open("GET", url);
		xhr.onreadystatechange = function () {
			if (xhr.readyState == 4) {
				if (xhr.status < 300) {
					let response = JSON.parse(xhr.responseText);

					if (response.error == true) {
						errorHandler(undefined, response.additionalInfo);
					} else {
						callback(response);
					}
				} else if (xhr.status == 429) {
					errorHandler(undefined, "Too many requests have been made from you recently; please wait a minute before making another request.");
				} else {
					errorHandler(undefined, xhr.responseText.toString());
				}
			}
		}
		xhr.send();
	} catch (e) {
		errorHandler(e);
	}
}
