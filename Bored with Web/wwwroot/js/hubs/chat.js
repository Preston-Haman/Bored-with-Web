
/**
 * The connection being used by SignalR to perform actions.
 */
var chatConnection;

/**
 * If the ability to send messages is enabled, or not.
 * The value of this should only be set through the setIsSendEnabled method.
 */
let isSendEnabled = false;

window.addEventListener("load", function () {
	setIsSendEnabled(false);

	initChatConnection();

	document.getElementById("chat-input").onkeyup = function (e) {
		if (e.key === "Enter") {
			sendMessage();
			document.getElementById("chat-input").focus();
		}
	};

	document.getElementById("chat-send").onclick = sendMessage;
});

/**
 * Sets isSendEnabled to the given value.
 * 
 * @param {Boolean} enabled - The value to set.
 */
function setIsSendEnabled(enabled) {
	document.getElementById("chat-send").disabled = !enabled;
	document.getElementById("chat-input").disabled = !enabled;
	isSendEnabled = enabled;
}

/**
 * Creates and starts the SignalR connection.
 */
function initChatConnection() {
	chatConnection = CHAT_CONNECTION_BUILDER.build();

	chatConnection.on(CHAT_RECEIVE_MESSAGE, receiveMessage);

	chatConnection.start().then(function () {
		setIsSendEnabled(true);
	}).catch(function (err) {
		setIsSendEnabled(false);
		return console.error(err.toString());
	});
}

/**
 * Sends a message to the server through SignalR. The content of the message is retrieved from the page.
 */
function sendMessage() {
	if (!isSendEnabled) return;

	let message = document.getElementById("chat-input").value;
	if (message) {
		chatConnection.invoke(CHAT_SEND_MESSAGE, message).then(function () {
			document.getElementById("chat-input").value = "";
		}).catch(function (err) {
			return console.error(err.toString());
		});
	}
}

/**
 * Appends a message to the chat messages with the given user as the sender.
 * 
 * @param {String} user - The name of the user sending a message.
 * @param {String} message - The message being sent.
 * @param {Boolean} isActiveUser - If true, the message was sent by the user.
 */
function receiveMessage(user, message, isActiveUser) {
	//<li><span class="chat-user-label user-select-none chat-active-user">user</span><span class="text-muted">message</span></li>
	let item = document.createElement("li");
	let userSpan = document.createElement("span");
	userSpan.classList.add("chat-user-label", "user-select-none");
	if (isActiveUser) {
		userSpan.classList.add("chat-active-user")
	}
	userSpan.innerText = user;
	item.appendChild(userSpan);

	let messageSpan = document.createElement("span");
	messageSpan.classList.add("text-muted");
	messageSpan.innerText = message;
	item.appendChild(messageSpan);

	let chatMessages = document.getElementById("chat-messages");
	let chatbox = chatMessages.parentElement;

	//Scroll the box if it's at least 95% of the way down...
	let scroll = chatbox.scrollTop >= (chatbox.scrollHeight - chatbox.clientHeight) * 0.95;

	chatMessages.appendChild(item);
	if (scroll) chatbox.scrollTop = chatbox.scrollHeight;
}
