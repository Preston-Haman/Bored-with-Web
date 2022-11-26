
/**
 * Creates a bootstrap alert and appends it as a child of an element with the id of "messages" if it exists.
 * 
 * @param {String} messageText - The text to display to the user within this message.
 * @param {String} messageType - The bootstrap color class to apply (primary, secondary, success, danger, warning, info, light, dark).
 * @param {Boolean} canDismiss - If true, there will be a button to dismiss the message.
 */
function createAndSendMessage(messageText, messageType = "info", canDismiss = true) {
	/*
	<div class="alert alert-success alert-dismissible fade show" role="alert">
		<span>Message Text</span>
		<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
	</div>
	*/
	let messages = document.getElementById("messages");
	if (messages) {
		let div = document.createElement("div");
		div.classList.add("alert", `alert-${messageType}`);
		div.setAttribute("role", "alert");

		let messageSpan = document.createElement("span");
		messageSpan.innerText = messageText;
		div.appendChild(messageSpan);

		if (canDismiss) {
			div.classList.add("alert-dismissible", "fade", "show");
			let closeButton = document.createElement("button");
			closeButton.setAttribute("type", "button");
			closeButton.classList.add("btn-close");
			closeButton.setAttribute("data-bs-dismiss", "alert");
			closeButton.setAttribute("aria-label", "Close");
			div.appendChild(closeButton);
		}

		messages.appendChild(div);
	}
}

/**
 * Creates a modal dialog and returns it with the following structure:
 * 
 * <div class="modal fade" tabindex="-1" aria-labelledby="exampleModalLabel" aria-hidden="true">
 * 		<div class="modal-dialog">
 * 			<div class="modal-content">
 * 				<div class="modal-header">
 * 					<h1 class="modal-title fs-5" id="modal-label">title</h1>
 * 					<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
 * 				</div>
 * 				<div class="modal-body">
 * 					<p>message</p>
 * 				</div>
 * 			</div>
 * 		</div>
 * 	</div>
 * 
 * @param {String} title - The title of the created modal.
 * @param {String} message - The message to display in the body of the modal.
 * @returns The created modal element.
 */
function createModal(title, message) {
	let modal = document.createElement("div");
	modal.classList.add("modal", "fade");
	modal.setAttribute("tabindex", "-1");
	modal.setAttribute("aria-labelledby", "modal-label");
	modal.setAttribute("aria-hidden", "true");
	/* div class="modal fade" */{
		let modalDialog = document.createElement("div");
		modalDialog.classList.add("modal-dialog");
		/* div class="modal-dialog" */{
			let modalContent = document.createElement("div");
			modalContent.classList.add("modal-content");
			/* div class="modal-content" */{
				let modalHeader = document.createElement("div");
				modalHeader.classList.add("modal-header");
				/* div class="modal-header" */{
					let modalTitle = document.createElement("h1");
					modalTitle.classList.add("modal-title", "fs-5");
					modalTitle.setAttribute("id", "modal-label");
					modalTitle.innerText = title;
					modalHeader.appendChild(modalTitle);

					let modalHeaderButton = document.createElement("button");
					modalHeaderButton.classList.add("btn-close");
					modalHeaderButton.setAttribute("type", "button");
					modalHeaderButton.setAttribute("data-bs-dismiss", "modal");
					modalHeaderButton.setAttribute("aria-label", "Close");
					modalHeader.appendChild(modalHeaderButton);
				}
				modalContent.appendChild(modalHeader);

				let modalBody = document.createElement("div");
				modalBody.classList.add("modal-body");
				/* div class="modal-body" */ {
					let messageParagraph = document.createElement("p");
					messageParagraph.innerText = message;
					modalBody.appendChild(messageParagraph);
				}
				modalContent.appendChild(modalBody);
			}
			modalDialog.appendChild(modalContent);
		}
		modal.appendChild(modalDialog);
	}

	return modal;
}
