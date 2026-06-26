namespace Vertex.Services.Data
{
    /// <summary>
    /// A single milestone entry within a category template.
    /// </summary>
    public class MilestoneEntry
    {
        public string Name { get; }
        public string Phase { get; }
        public int PhaseOrder { get; }
        public string[] SuggestedSkills { get; }

        public MilestoneEntry(string name, string phase, int phaseOrder, string[] suggestedSkills)
        {
            Name = name;
            Phase = phase;
            PhaseOrder = phaseOrder;
            SuggestedSkills = suggestedSkills;
        }
    }

    /// <summary>
    /// Template containing milestones and risk hints for a project category.
    /// </summary>
    public class CategoryTemplate
    {
        public MilestoneEntry[] Milestones { get; set; } = Array.Empty<MilestoneEntry>();
        public string[] RiskHints { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Static registry of milestone templates per project category.
    /// These are injected into the AI prompt BEFORE sending to Gemini,
    /// so the AI uses them as a backbone rather than generating from scratch.
    /// 
    /// Categories "Other" and "Auto detect" are NOT in this registry —
    /// they fall back to 100% AI generation.
    /// </summary>
    public static class MilestoneTemplates
    {
        public static readonly Dictionary<string, CategoryTemplate> Templates = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Software"] = new CategoryTemplate
            {
                Milestones = new[]
                {
                    new MilestoneEntry("Requirements & Planning", "early", 1,
                        new[] { "project-management", "business-analysis" }),
                    new MilestoneEntry("Architecture & Environment Setup", "early", 2,
                        new[] { "backend", "devops", "system-design" }),
                    new MilestoneEntry("Core Feature Development", "mid", 3,
                        new[] { "frontend", "backend", "fullstack" }),
                    new MilestoneEntry("API & Integration", "mid", 4,
                        new[] { "backend", "api-design", "database" }),
                    new MilestoneEntry("Testing & QA", "late", 5,
                        new[] { "qa", "testing", "automation" }),
                    new MilestoneEntry("UI/UX Polish & Optimization", "late", 6,
                        new[] { "frontend", "ui-design", "performance" }),
                    new MilestoneEntry("Deployment & Launch", "final", 7,
                        new[] { "devops", "project-management", "documentation" }),
                },
                RiskHints = new[]
                {
                    "Scope creep due to unclear or evolving requirements",
                    "Technical debt accumulating from rapid prototyping under tight deadlines",
                    "Integration delays between frontend and backend teams",
                    "Insufficient test coverage leading to regression bugs at launch",
                    "Dependency on third-party APIs or services causing unexpected blockers"
                }
            },

            ["Design"] = new CategoryTemplate
            {
                Milestones = new[]
                {
                    new MilestoneEntry("Research & Creative Brief", "early", 1,
                        new[] { "ux-research", "market-research" }),
                    new MilestoneEntry("Mood Board & Concept Exploration", "early", 2,
                        new[] { "visual-design", "branding", "creative-direction" }),
                    new MilestoneEntry("Wireframing & Layout", "mid", 3,
                        new[] { "ui-design", "wireframing", "information-architecture" }),
                    new MilestoneEntry("Visual Design & Asset Creation", "mid", 4,
                        new[] { "visual-design", "illustration", "graphic-design" }),
                    new MilestoneEntry("Prototyping & Interaction Design", "late", 5,
                        new[] { "prototyping", "interaction-design", "animation" }),
                    new MilestoneEntry("User Testing & Iteration", "late", 6,
                        new[] { "ux-research", "usability-testing", "feedback-analysis" }),
                    new MilestoneEntry("Final Delivery & Handoff", "final", 7,
                        new[] { "design-systems", "documentation", "asset-management" }),
                },
                RiskHints = new[]
                {
                    "Subjective feedback loops delaying design approval",
                    "Scope expansion from stakeholder requests mid-project",
                    "Inconsistent brand application across deliverables",
                    "Insufficient user testing leading to poor usability",
                    "Asset delivery delays blocking downstream development"
                }
            },

            ["Research"] = new CategoryTemplate
            {
                Milestones = new[]
                {
                    new MilestoneEntry("Topic Definition & Literature Review", "early", 1,
                        new[] { "academic-writing", "critical-thinking", "literature-review" }),
                    new MilestoneEntry("Research Methodology Design", "early", 2,
                        new[] { "research-methodology", "statistical-modeling", "survey-design" }),
                    new MilestoneEntry("Data Collection & Fieldwork", "mid", 3,
                        new[] { "data-collection", "interviewing", "survey-administration" }),
                    new MilestoneEntry("Data Analysis & Interpretation", "mid", 4,
                        new[] { "data-analysis", "statistics", "data-visualization" }),
                    new MilestoneEntry("Report Writing & Documentation", "late", 5,
                        new[] { "academic-writing", "technical-writing", "LaTeX" }),
                    new MilestoneEntry("Peer Review & Revision", "late", 6,
                        new[] { "peer-review", "editing", "quality-assurance" }),
                    new MilestoneEntry("Final Submission & Presentation", "final", 7,
                        new[] { "presentation", "public-speaking", "documentation" }),
                },
                RiskHints = new[]
                {
                    "Difficulty accessing sufficient or quality data sources",
                    "Methodology flaws discovered late requiring redesign",
                    "Timeline pressure compromising data analysis depth",
                    "Peer review feedback requiring significant rewrites",
                    "Ethical approval delays for human-subject research"
                }
            },

            ["Marketing"] = new CategoryTemplate
            {
                Milestones = new[]
                {
                    new MilestoneEntry("Market Research & Audience Analysis", "early", 1,
                        new[] { "market-research", "data-analysis", "competitive-analysis" }),
                    new MilestoneEntry("Strategy & Campaign Planning", "early", 2,
                        new[] { "strategic-planning", "brand-strategy", "budget-management" }),
                    new MilestoneEntry("Content Creation & Asset Production", "mid", 3,
                        new[] { "copywriting", "graphic-design", "video-production" }),
                    new MilestoneEntry("Campaign Setup & Channel Configuration", "mid", 4,
                        new[] { "digital-marketing", "seo", "social-media", "ads-management" }),
                    new MilestoneEntry("Launch & Distribution", "late", 5,
                        new[] { "campaign-management", "email-marketing", "social-media" }),
                    new MilestoneEntry("Monitoring & A/B Testing", "late", 6,
                        new[] { "analytics", "a-b-testing", "data-analysis" }),
                    new MilestoneEntry("Reporting & Optimization", "final", 7,
                        new[] { "reporting", "data-visualization", "strategic-planning" }),
                },
                RiskHints = new[]
                {
                    "Target audience assumptions proving incorrect after launch",
                    "Content production bottlenecks delaying campaign timeline",
                    "Budget overruns on paid advertising channels",
                    "Low engagement rates requiring mid-campaign pivot",
                    "Platform algorithm changes affecting organic reach"
                }
            },

            ["Business"] = new CategoryTemplate
            {
                Milestones = new[]
                {
                    new MilestoneEntry("Market Analysis & Opportunity Assessment", "early", 1,
                        new[] { "market-research", "competitive-analysis", "data-analysis" }),
                    new MilestoneEntry("Business Plan & Strategy Development", "early", 2,
                        new[] { "strategic-planning", "business-development", "writing" }),
                    new MilestoneEntry("Financial Modeling & Budgeting", "mid", 3,
                        new[] { "financial-analysis", "accounting", "excel", "forecasting" }),
                    new MilestoneEntry("Stakeholder Engagement & Partnerships", "mid", 4,
                        new[] { "negotiation", "stakeholder-management", "communication" }),
                    new MilestoneEntry("Implementation & Operations Setup", "late", 5,
                        new[] { "operations-management", "project-management", "process-design" }),
                    new MilestoneEntry("Risk Assessment & Compliance Review", "late", 6,
                        new[] { "risk-assessment", "legal", "compliance", "quality-assurance" }),
                    new MilestoneEntry("Pitch Presentation & Final Review", "final", 7,
                        new[] { "presentation", "pitch-deck", "public-speaking" }),
                },
                RiskHints = new[]
                {
                    "Market conditions shifting during planning phase",
                    "Financial projections based on optimistic assumptions",
                    "Key stakeholder or partner withdrawal mid-project",
                    "Regulatory or compliance issues discovered late",
                    "Insufficient competitive differentiation in final strategy"
                }
            },
        };

        /// <summary>
        /// Tries to get the template for a given category.
        /// Returns false for "Other", "Auto detect", or unknown categories.
        /// </summary>
        public static bool TryGetTemplate(string category, out CategoryTemplate? template)
        {
            if (string.IsNullOrEmpty(category)
                || string.Equals(category, "Other", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "Auto detect", StringComparison.OrdinalIgnoreCase))
            {
                template = null;
                return false;
            }

            return Templates.TryGetValue(category, out template);
        }
    }
}
