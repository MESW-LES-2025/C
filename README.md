# Legal System - Messaging Module Specification

## [FEATURE] Client-Lawyer Secure Messaging

As a Lawyer or Client, I want to exchange secure text messages within the application so that communication regarding legal processes is centralized, traceable, and faster than traditional email.

This feature focuses on a **Real-Time Messaging System** utilizing WebSockets. Due to strict project timelines (4-week deadline) and to ensure data integrity/traceability, this feature is strictly limited to **Create (Send)** and **Read (View)** operations. 

**Editing or Deleting messages is strictly prohibited** at this stage to maintain a complete, immutable audit trail of communications.

<hr />

### Acceptance Criteria (AC)

**Business**

- [ ] **Immutable History:** Users can send messages and view past messages. Once sent, a message **cannot** be edited or deleted by the sender or the receiver.
- [ ] **Real-Time Communication:** When a user is logged in, they must receive new messages instantly without refreshing the page.
- [ ] **Access Control:** - [ ] Clients can only message the Lawyer responsible for their specific cases.
    - [ ] Lawyers can message any Client they are associated with.
- [ ] **Chronological Order:** Messages must be displayed in strict chronological order (oldest at top, newest at bottom).
- [ ] **Visual Distinction:** The UI must clearly distinguish between "My Messages" (aligned right/distinct color) and "Received Messages" (aligned left).

**Tech**

- [ ] **WebSocket Implementation:** - [ ] The WebSocket connection must be initialized immediately upon a successful User Login.
    - [ ] The connection must be authenticated (using the user's JWT or session token).
- [ ] **Persistence:** All messages must be persisted to the database (SQL/NoSQL) to ensure history is available across sessions.
- [ ] **Data Model:** The Message entity must contain: `SenderID`, `ReceiverID`, `Timestamp`, `Content`, and `RelatedProcessID` (optional, if context is needed).
- [ ] **Endpoint Security:** REST endpoints (for history) and WS events must validate that the `Sender` and `Receiver` have a valid relationship.

<hr />

### Definition of Ready (DoR)

**Business**

- [ ] **Traceability:** This Feature is new and has no upstream dependencies, but relates to the User Management module (requires active Users).
- [ ] **Scope Limitation:** The restriction to "Create/Read Only" is formally accepted by the PO and stakeholders.
- [ ] **Decomposition:** The Feature is broken down into atomic User Stories:
    > - [ ] Send New Message;
    > - [ ] View Conversation History;
    > - [ ] Real-time Reception (WebSocket Client);
- [ ] **Design:** Wireframes for the Chat Window (floating or dedicated page) are approved.

**Tech**

- [ ] **Architecture Decision:** The specific WebSocket library (e.g., Socket.io, SignalR, or native WS) is selected.
- [ ] **DB Schema:** The `MESSAGE` table/collection schema is defined.

<hr />

### Definition of Done (DoD)

**Business**

- [ ] All User Stories are implemented.
- [ ] Lawyers and Clients can successfully exchange messages in real-time.
- [ ] History loads correctly for offline messages.
- [ ] User-facing documentation mentions that messages cannot be deleted for legal security reasons.

**Tech**

- [ ] Unit tests cover 75% of messaging logic.
- [ ] Integration tests validate the flow: Login -> Connect WS -> Send -> Receive -> Persist.
- [ ] Load testing: Basic validation that the WS server handles concurrent connections.
- [ ] Deployed to Staging/QA.

---

## [US] Messaging - Send New Message

As a logged-in user (Lawyer or Client), I want to send a text message to a specific recipient so that I can provide updates or ask questions immediately.

<hr />

### Acceptance Criteria (AC)

**Business**

- [ ] **Input Validation:** The message cannot be empty or consist only of whitespace.
- [ ] **Sending Action:** Upon clicking "Send" (or pressing Enter), the message is transmitted immediately.
- [ ] **Feedback:** The user sees a visual indicator (e.g., message appears in the chat list immediately) confirming the send action.
- [ ] **Constraint:** The user cannot delete or edit the message after sending.

**Tech**

- [ ] **WebSocket Event:** The client emits a `sendMessage` event (or similar) via the established WebSocket connection.
- [ ] **Server Validation:** The backend validates that the `Sender` has permission to message the `Receiver`.
- [ ] **Persistence:** The message is saved to the database *before* or *concurrently* with being relayed to the recipient to ensure no data loss.
- [ ] **Error Handling:** If the WebSocket connection is lost, the UI should prevent sending or show a "Reconnecting..." state.

### Tasks
- [ ] Create `POST /messages` endpoint or WS Event Handler `on('message')`.
- [ ] Implement backend validation (User A can talk to User B).
- [ ] Implement DB insertion logic.
- [ ] Create Frontend UI Input component (Text area + Send Button).
- [ ] Implement Frontend "Send" logic (emit event).

<hr />

### Definition of Ready (DoR)

**Business**
- [ ] Design: UI for the input area is approved.
- [ ] Rules: Max character limit (if any) is defined.

**Tech**
- [ ] WebSocket server is set up.
- [ ] Database `Messages` table is created.

### Definition of Done (DoD)
- [ ] Unit tests for the "Send" logic.
- [ ] Manual test: Send message, verify it appears in DB.

---

## [US] Messaging - View Conversation History

As a user, I want to view the full history of messages exchanged with a specific person so that I can reference past instructions and discussions.

<hr />

### Acceptance Criteria (AC)

**Business**

- [ ] **Load on Demand:** When opening a chat with a specific user, the previous message history loads automatically.
- [ ] **Ordering:** Messages are sorted by Timestamp ASC (oldest at top).
- [ ] **Formatting:**
    - [ ] My messages: Right-aligned.
    - [ ] Their messages: Left-aligned.
    - [ ] Timestamps are visible (e.g., on hover or below text).
- [ ] **Pagination:** If the history is long, the system loads the most recent X messages, allowing the user to scroll up to load more (Infinite Scroll).

**Tech**

- [ ] **REST API Endpoint:** A `GET /messages/{conversationId}` (or similar) endpoint returns the paginated list. (REST is often preferred over WS for fetching large history blocks).
- [ ] **Performance:** The query must be indexed by `RelatedProcessID` or `Sender/Receiver` pair and `Timestamp`.

### Tasks
- [ ] Create backend query to fetch messages between two users.
- [ ] Implement pagination (Limit/Offset or Cursor-based).
- [ ] Create `GET` endpoint for history.
- [ ] Create Frontend "Message Bubble" component.
- [ ] Implement "Scroll to bottom" logic on load.

<hr />

### Definition of Ready (DoR)

**Business**
- [ ] UI Design for the chat window and message bubbles (sent vs received) is approved.

**Tech**
- [ ] Seed data exists in the database to test history loading.

### Definition of Done (DoD)
- [ ] Infinite scroll or "Load More" works smoothly.
- [ ] Styling matches the approved mockups.

---

## [US] Messaging - Real-Time Connection & Reception

As a user, I want the system to automatically connect me to the messaging service upon login and display new messages instantly, so that I don't miss urgent communications.

<hr />

### Acceptance Criteria (AC)

**Business**

- [ ] **Auto-Connect:** The chat feature becomes active immediately after the user logs into the dashboard.
- [ ] **Instant Update:** When User A sends a message, User B (if online) sees it appear in their chat window within seconds, without refreshing the page.
- [ ] **Notification:** If the chat window is closed/minimized within the app, a visual badge (e.g., "1 New Message") appears.

**Tech**

- [ ] **Socket Initialization:** The Frontend initializes the WebSocket client using the user's Auth Token.
- [ ] **Room/Channel Logic:** On connect, the server joins the socket to a specific room (e.g., `user_{id}`) to allow targeted message delivery.
- [ ] **Event Listening:** The client listens for `receiveMessage` events and appends the payload to the current local state/UI.
- [ ] **Connection Handling:**
    - [ ] Handle `disconnect` (User closes tab).
    - [ ] Handle `reconnect` (Network blip).

### Tasks
- [ ] Configure WebSocket Server (Backend).
- [ ] Implement JWT verification for WS connection handshake.
- [ ] Implement Frontend WebSocket Context/Service (Global state).
- [ ] Implement `socket.on('receive_message')` listener in Frontend.
- [ ] Update UI state when a new message arrives.

<hr />

### Definition of Ready (DoR)

**Business**
- [ ] Security requirement: Only authenticated users can establish a connection.

**Tech**
- [ ] API Contract for WS events (`emit` payload and `listen` payload) is defined.

### Definition of Done (DoD)
- [ ] Validated with two different browsers (User A and User B) chatting in real-time.
- [ ] Verify that no messages are lost if the user is online.